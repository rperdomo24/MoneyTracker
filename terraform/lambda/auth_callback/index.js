const fetch = require("node-fetch");
const admin = require("firebase-admin");
const serviceAccount = require("./firebase-service-account.json");

exports.handler = async (event) => {
    try {
        const queryParams = event.rawQueryString || "";
        const urlParams = new URLSearchParams(queryParams);
        const code = urlParams.get("code");

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

        if (!userData.email) {
            console.error("No email returned from userinfo endpoint.", userData);
            return { statusCode: 400, body: JSON.stringify({ message: "No email returned from userinfo endpoint.", userData }) };
        }

        // 2.5. Inicializar Firebase Admin SDK
        admin.initializeApp({
            credential: admin.credential.cert(serviceAccount),
        });

        // 3. Guardar en Firestore
        const db = admin.firestore();

        try {
            const firestorePayload = {
                email: userData.email,
                refresh_token: tokenData.refresh_token,
                access_token: tokenData.access_token,
                expires_in: tokenData.expires_in,
                creation_timestamp: Date.now(),
                provider: "google",
            };
            await db.collection("oauth_tokens").doc(firestorePayload.email).set(firestorePayload);
            console.log(`Token guardado exitosamente en Firestore para ${firestorePayload.email}`);
        } catch (firebaseError) {
            console.error("Firestore error:", firebaseError);
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
            body: `Ha iniciado sesión con ${userData.email}. Puede cerrar esta ventana.`,
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