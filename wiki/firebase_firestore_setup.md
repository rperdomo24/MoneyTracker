# Configuración Manual de Firebase Firestore para Guardar Tokens OAuth

Este documento describe cómo configurar manualmente una base de datos Firebase Firestore para almacenar `refresh_token` y `access_token` de usuarios autenticados con Gmail.

---

## ✅ 1. Crear proyecto en Firebase

1. Ve a [https://console.firebase.google.com/](https://console.firebase.google.com/)
2. Haz clic en **"Agregar proyecto"**
3. Usa un nombre como `gmail-oauth-tokens`
4. Selecciona **"No habilitar Google Analytics"** (opcional)
5. Finaliza y espera a que se cree el proyecto

---

## ✅ 2. Activar Firestore

1. Ve a **"Cloud Firestore" > "Crear base de datos"**
2. Elige:
   - **Modo nativo**
   - **Ubicación** (recomendado: `us-central` o más cercana)
3. Completa el proceso

---

## ✅ 3. Crear una cuenta de servicio

1. Ve a: [https://console.cloud.google.com/iam-admin/serviceaccounts](https://console.cloud.google.com/iam-admin/serviceaccounts)
2. Selecciona el proyecto creado
3. Haz clic en **"Crear cuenta de servicio"**
   - Nombre: `lambda-gmail-oauth`
   - Rol: **"Editor"**
4. Haz clic en crear
5. En la lista de cuentas, haz clic en `⋮` → **"Administrar claves"**
6. Agrega una nueva clave → selecciona **JSON**
7. Descarga el archivo `.json` (muy importante, úsalo en tu Lambda)

---

## ✅ 4. Crear colección de prueba

1. Ir a **Cloud Firestore**
2. Crear una colección: `oauth_tokens`
3. Agrega un documento de ejemplo:

```json
{
  "email": "usuario@gmail.com",
  "refresh_token": "abc123...",
  "provider": "google"
}
```

---

## ✅ 5. Reglas de seguridad (modo desarrollo)

Para pruebas:

```plaintext
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /{document=**} {
      allow read, write: if true;
    }
  }
}
```

📌 Recuerda actualizar estas reglas para producción.

---

## ✅ 6. Integrar con Lambda

1. Guarda el archivo `.json` descargado como:
   `lambda/auth_callback/firebase-service-account.json`
2. Usa `firebase-admin` desde tu Lambda para conectarte a Firestore
