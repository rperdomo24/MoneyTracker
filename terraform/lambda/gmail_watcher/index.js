const { Buffer } = require("buffer");

const ALLOWED_SENDERS = [
    "notificaciones@bancocuscatlan.com",
    "info@baccredomatic.com",
    "josuemercally@outlook.com"
];

async function getAccessTokenFromRefreshToken(refresh_token) {
    const fetch = require("node-fetch");

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

    return tokenJson.access_token;
}

async function fetchMessageDetails(msgId, accessToken) {
    const fetch = require("node-fetch");

    const msgRes = await fetch(`https://gmail.googleapis.com/gmail/v1/users/me/messages/${msgId}?format=full`, {
        headers: { Authorization: `Bearer ${accessToken}` },
    });

    return await msgRes.json();
}

async function getAllowedMessagesFromHistory(historyJson, accessToken, allowedSenders) {
    const messageIds = new Set();

    console.log("History JSON:", historyJson);
    if (historyJson.history) {
        for (const h of historyJson.history) {
            if (h.messagesAdded) {
                for (const entry of h.messagesAdded) {
                    if (entry.message && entry.message.id) {
                        messageIds.add(entry.message.id);
                    }
                }
            }
        }
    }

    console.log("Mensajes nuevos detectados:", Array.from(messageIds));

    const matchedMessages = [];

    for (const msgId of messageIds) {
        try {
            const msgJson = await fetchMessageDetails(msgId, accessToken);
            const { headers } = msgJson.payload;
            const fromHeader = headers.find(h => h.name.toLowerCase() === "from");
            const fromValue = fromHeader ? fromHeader.value.toLowerCase() : "";

            const isAllowed = allowedSenders.some(allowed => fromValue.includes(allowed.toLowerCase()));

            if (isAllowed) {
                console.log(`Correo permitido de: ${fromValue}`);
                matchedMessages.push({
                    id: msgId,
                    from: fromValue,
                    snippet: msgJson.snippet,
                });
            } else {
                console.log(`Correo ignorado de: ${fromValue}`);
            }
        } catch (msgErr) {
            console.error(`Error procesando mensaje ${msgId}:`, msgErr.stack || msgErr);
        }
    }

    return matchedMessages;
}

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

        const { refresh_token, lastHistoryId } = docSnap.data();

        if (!lastHistoryId) {
            console.log("No hay lastHistoryId previo. Se usará el recibido:", historyId);
        }

        const accessToken = await getAccessTokenFromRefreshToken(refresh_token);

        const startHistoryId = lastHistoryId || historyId;

        const historyRes = await fetch(`https://gmail.googleapis.com/gmail/v1/users/me/history?startHistoryId=${startHistoryId}`, {
            headers: { Authorization: `Bearer ${accessToken}` }
        });

        const historyJson = await historyRes.json();
        console.log("Respuesta de historial:", JSON.stringify(historyJson, null, 2));

        const matchedMessages = await getAllowedMessagesFromHistory(historyJson, accessToken, ALLOWED_SENDERS);
        console.log("Mensajes seleccionados para análisis:", matchedMessages);

        // Actualizar Firestore con nuevo historyId
        await db.collection("oauth_tokens").doc(emailAddress).update({
            lastHistoryId: historyId,
        });
        console.log("HistoryId actualizado a:", historyId);

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