# Configuración de acceso OAuth a Gmail API (modo test)

Este documento describe cómo configurar el acceso de lectura a la Gmail API usando OAuth 2.0 en modo de desarrollo o pruebas.

---

## ✅ Paso 1: Crear un proyecto en Google Cloud

1. Ve a [https://console.cloud.google.com/](https://console.cloud.google.com/)
2. Crea un nuevo proyecto (ej. `gmail-ai-lab`)
3. Actívalo como proyecto actual

---

## ✅ Paso 2: Habilitar APIs necesarias

En el menú de la izquierda:

1. Ir a **API y servicios > Biblioteca**
2. Buscar y habilitar:
   - **Gmail API**
   - (Opcional) **Pub/Sub API** si usarás notificaciones automáticas

---

## ✅ Paso 3: Configurar la pantalla de consentimiento OAuth

1. Ir a **API y servicios > Pantalla de consentimiento OAuth**
2. Tipo de usuario:
   - `Interno` si es tu cuenta personal y del mismo dominio (Google Workspace)
   - `Externo` si es una cuenta de Gmail común
3. Rellenar:
   - Nombre de la app (ej. Gmail Categorizer)
   - Email de soporte y desarrollador
4. Scopes requeridos:
   - `https://www.googleapis.com/auth/gmail.readonly` (solo lectura)
   - o `https://www.googleapis.com/auth/gmail.modify` si modificarás etiquetas

---

## ✅ Paso 4: Crear credenciales OAuth 2.0

1. Ir a **API y servicios > Credenciales**
2. Crear credencial > **ID de cliente OAuth 2.0**
3. Tipo: Aplicación Web
4. URLs de redireccionamiento autorizadas:
   - Ejemplo: `http://localhost:3000/callback`

Guarda el `client_id` y `client_secret`.

---

## ✅ Paso 5: Flujo de autorización manual (modo test)

### 1. Generar URL de autorización

```text
https://accounts.google.com/o/oauth2/v2/auth?
 client_id=TU_CLIENT_ID
 &redirect_uri=http://localhost:3000/callback
 &response_type=code
 &scope=https://www.googleapis.com/auth/gmail.readonly
 &access_type=offline
 &prompt=consent
```

### 2. Intercambiar el code por tokens

```bash
curl -X POST https://oauth2.googleapis.com/token \
  -d client_id=TU_CLIENT_ID \
  -d client_secret=TU_CLIENT_SECRET \
  -d code=EL_CODE_DE_LA_REDIRECCION \
  -d redirect_uri=http://localhost:3000/callback \
  -d grant_type=authorization_code
```

Esto te dará:
- `access_token`
- `refresh_token`
- `expires_in`

✅ Guarda esos tokens para usarlos desde Lambda.


# Secrets:

Google ClientID: 495071281865-etg2ae882bdvm02pji30j5c9hs1uj85e.apps.googleusercontent.com
Google SecretKey: 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