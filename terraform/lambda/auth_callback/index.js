const fetch = require("node-fetch");
const admin = require("firebase-admin");

// Inicializar Firebase Admin SDK solo una vez
let initialized = false;
function initFirebase() {
    if (!initialized) {
        const serviceAccount = require("./firebase-service-account.json");

        admin.initializeApp({
            credential: admin.credential.cert(serviceAccount),
        });
        initialized = true;
    }
}

exports.handler = async (event) => {
    console.log("STEP 1: Recibido evento:", event);
    try {
        const queryParams = event.rawQueryString || "";
        const urlParams = new URLSearchParams(queryParams);
        const code = urlParams.get("code");
        console.log("STEP 2: Código recibido:", code);

        if (!code) {
            return { statusCode: 400, body: "Missing authorization code" };
        }

        const client_id = process.env.GOOGLE_CLIENT_ID;
        const client_secret = process.env.GOOGLE_CLIENT_SECRET;
        const redirect_uri = process.env.GOOGLE_REDIRECT_URI;

        // 1. Intercambiar code por tokens
        const tokenRes = await fetch("https://oauth2.googleapis.com/token", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams({
                code,
                client_id,
                client_secret,
                redirect_uri,
                grant_type: "authorization_code",
            }),
        });

        const tokenData = await tokenRes.json();
        console.log("STEP 3: Tokens recibidos:", tokenData);

        if (tokenRes.status !== 200) {
            console.error("Token endpoint error:", tokenData);
            return {
                statusCode: 500,
                body: JSON.stringify({ error: "Error getting token", details: tokenData }),
            };
        }

        if (!tokenData.refresh_token) {
            return {
                statusCode: 400,
                body: JSON.stringify({
                    error: "No refresh_token returned. Try with prompt=consent again.",
                }),
            };
        }

        // 2. Obtener información del usuario
        const userRes = await fetch("https://www.googleapis.com/oauth2/v2/userinfo", {
            headers: { Authorization: `Bearer ${tokenData.access_token}` },
        });

        const userData = await userRes.json();
        console.log("STEP 4: Datos de usuario:", userData);

        if (!userData.email) {
            console.error("No email returned from userinfo endpoint.", userData);
            return { statusCode: 400, body: JSON.stringify({ message: "No email returned from userinfo endpoint.", userData }) };
        }

        // 3. Guardar en Firestore
        initFirebase();
        const db = admin.firestore();

        console.log("STEP 5: Guardando en Firestore...");
        try {
            await db.collection("oauth_tokens").doc(userData.email).set({
                email: userData.email,
                refresh_token: tokenData.refresh_token,
                access_token: tokenData.access_token,
                expires_in: tokenData.expires_in,
                creation_timestamp: Date.now(),
                provider: "google",
            });
            console.log("STEP 6: Guardado con éxito en Firestore.");
        } catch (firebaseError) {
            console.error("Firestore error:", firebaseError.stack || firebaseError);
            return {
                statusCode: 500,
                body: JSON.stringify({
                    message: "Error writing to Firestore",
                    error: firebaseError.message,
                    stack: firebaseError.stack,
                }),
            };
        }

        return {
            statusCode: 200,
            body: `Token saved for user ${userData.email}`,
        };
    } catch (err) {
        console.error("OAuth Callback Error:", err.stack || err);
        return {
            statusCode: 500,
            body: JSON.stringify({
                message: "Internal Server Error",
                error: err.message,
                stack: err.stack
            }),
        };
    }
};