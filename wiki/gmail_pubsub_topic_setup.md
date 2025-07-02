# Configuración de Google Pub/Sub para Gmail Watch

Este documento explica cómo configurar un Topic de Google Pub/Sub para que Gmail API pueda enviar notificaciones sobre nuevos correos.

---

## ✅ Requisitos previos

- Tener un proyecto de Google Cloud habilitado.
- Tener acceso a la [Consola de Google Cloud](https://console.cloud.google.com).
- Haber creado una cuenta de servicio con OAuth2 previamente (usada para autenticar Gmail API).
- Haber habilitado la API de Pub/Sub y la API de Gmail.

---

## 🧩 Paso 1: Crear el Topic de Pub/Sub

1. Ve a: [https://console.cloud.google.com/cloudpubsub](https://console.cloud.google.com/cloudpubsub)
2. Haz clic en **"Create Topic"**.
3. Ingresa el ID del topic:  
   ```
   gmail_push_topic
   ```
4. Haz clic en **Create**.

---

## 🛡️ Paso 2: Dar permisos de publicación a Gmail API

Gmail necesita poder publicar mensajes en tu topic.

1. Copia el email de cuenta de servicio de Gmail que comienza con:

   ```
   gmail-api-push@system.gserviceaccount.com
   ```

2. Ve al topic creado → pestaña **Permissions** → **Add Principal**.
3. Ingresa el email:  
   ```
   gmail-api-push@system.gserviceaccount.com
   ```
4. Asigna el rol:  
   ```
   Pub/Sub Publisher
   ```

5. Guarda los cambios.

---

## 📌 Paso 3: Nota sobre `topicName` en Gmail Watch

Cuando configures la llamada a:

```
POST https://gmail.googleapis.com/gmail/v1/users/me/watch
```

Tu `topicName` debe ser exactamente así:

```
projects/<PROJECT_ID>/topics/gmail_push_topic
```

Reemplaza `<PROJECT_ID>` con el ID real de tu proyecto de Google Cloud.

---

## 🧪 Verificación

Puedes usar la pestaña **Messages** del topic para ver si llegan notificaciones cuando ingresan nuevos correos en la cuenta conectada.

---

## 🚀 Paso 4: Crear suscripción Push al topic

Para recibir notificaciones en tu Lambda desde Pub/Sub, debes crear una suscripción tipo "Push":

1. Ve al topic `gmail_push_topic` en [Google Pub/Sub](https://console.cloud.google.com/cloudpubsub)
2. Haz clic en **"Create Subscription"**
3. Ingresa un ID para la suscripción, por ejemplo:  
   ```
   gmail_push_subscription
   ```
4. Tipo de entrega: selecciona **Push**
5. En el campo **Push endpoint**, pega la URL pública de tu Lambda `gmail_watcher`, por ejemplo:  
   ```
   https://<api-id>.execute-api.<region>.amazonaws.com/prod/
   ```
6. Asegúrate de que el formato de entrega esté en **JSON**
7. Haz clic en **Create**

Una vez creada, Google enviará notificaciones POST a tu endpoint cada vez que llegue un correo nuevo y Gmail lo publique en el tópico.
