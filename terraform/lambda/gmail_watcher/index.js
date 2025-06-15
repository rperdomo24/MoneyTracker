const { Buffer } = require("buffer");

exports.handler = async (event) => {
    console.log("Gmail watcher Lambda invocada");

    try {
        const body = JSON.parse(event.body);
        console.log("Evento Pub/Sub recibido:", body);

        const { message } = body;
        const data = Buffer.from(message.data, "base64").toString("utf-8");
        const parsed = JSON.parse(data);

        console.log("Notificación decodificada:", parsed);

        const { emailAddress, historyId } = parsed;

        const admin = require("firebase-admin");
        const fetch = require("node-fetch");

        const serviceAccount = require("./firebase-service-account.json");

        if (!admin.apps.length) {
            admin.initializeApp({
                credential: admin.credential.cert(serviceAccount),
            });
        }

        const db = admin.firestore();

        console.log("Buscando token en Firestore para:", emailAddress);
        const docSnap = await db.collection("oauth_tokens").doc(emailAddress).get();

        if (!docSnap.exists) {
            throw new Error(`No se encontró token para ${emailAddress}`);
        }

        const { refresh_token } = docSnap.data();

        // Obtener nuevo access token usando refresh_token
        const tokenRes = await fetch("https://oauth2.googleapis.com/token", {
            method: "POST",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams({
                client_id: process.env.GOOGLE_CLIENT_ID,
                client_secret: process.env.GOOGLE_CLIENT_SECRET,
                refresh_token,
                grant_type: "refresh_token",
            }),
        });

        const tokenJson = await tokenRes.json();

        if (!tokenRes.ok) {
            console.error("Error al refrescar token:", tokenJson);
            throw new Error(tokenJson.error_description || "Error al refrescar token");
        }

        const accessToken = tokenJson.access_token;

        // Llamar a la API de Gmail para consultar historial
        const historyRes = await fetch(`https://gmail.googleapis.com/gmail/v1/users/me/history?startHistoryId=${historyId}`, {
            headers: { Authorization: `Bearer ${accessToken}` }
        });

        const historyJson = await historyRes.json();

        console.log("Respuesta de historial:", JSON.stringify(historyJson, null, 2));

        console.log(`Correo notificado: ${emailAddress}, historyId: ${historyId}`);

        return {
            statusCode: 200,
            body: JSON.stringify({ success: true }),
        };
    } catch (err) {
        console.error("Error procesando notificación:", err.stack || err);
        return {
            statusCode: 500,
            body: JSON.stringify({ error: "Error procesando notificación", details: err.message }),
        };
    }
};