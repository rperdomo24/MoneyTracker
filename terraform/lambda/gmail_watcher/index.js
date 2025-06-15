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