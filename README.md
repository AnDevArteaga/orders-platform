# Orders Platform

API para que el sistema de un comercio cree pedidos y cobre, sin guardar tarjetas. El comprador paga en la página de Wompi. Esta API lleva el pedido, le pide el enlace de pago a la pasarela y le avisa al comercio cuando el resultado llega.

Quien integra es el backend de una tienda o un ERP. El comprador no tiene cuenta aquí.

## Cómo se paga

La tienda le dice a esta API: cobra este pedido, por este monto, a este correo. En ese mensaje no va el número de tarjeta.

En sandbox la API devuelve una URL de prueba. Abrirla y aprobar no llama a ningún banco. También se puede cerrar el resultado en la misma petición, con `sandbox_result`.

En producción la API le pide a Wompi un enlace de pago, con las credenciales de ese comercio, y devuelve `https://checkout.wompi.co/l/...`. El navegador del comprador va ahí. Wompi cobra y le avisa a esta API. Esta API comprueba la firma y el monto: si el valor cobrado no es el total del pedido, el pedido no queda pagado. Después le manda un webhook firmado al comercio.

La llave de producción no crea cobros hasta que el comercio guarda sus credenciales y activa la integración. Esas credenciales se guardan cifradas y ninguna respuesta las devuelve. Volver a guardarlas apaga la activación.

## Qué cubre

Registro del comercio, login y API keys. La llave en claro se muestra una vez. Sandbox (`sk_test_`) y producción (`sk_live_`). Clientes, pedidos y su historial de estados. Cobro, webhook de salida con tres intentos, webhook de entrada de Wompi, registro de cada request a `/v1` y un límite por API key.

Estados del pedido: `draft`, `pending_payment`, `paid`, `payment_failed`, `fulfilled`, `cancelled`. Solo se despacha un pedido pagado. El comercio no puede marcar un pedido como pagado: lo decide el simulador o Wompi.

La moneda de esta versión es COP, en pesos enteros.

## Arranque

Hace falta el SDK de .NET 10.

```sh
dotnet test
dotnet run --project Orders.Api
```

La API escucha en `http://localhost:5080`. Swagger queda en `/swagger`. El estado local es un SQLite, `orders.db`, en el directorio de trabajo. No hace falta SQL Server.

La clave de firma y la clave con la que se cifran los secretos de Wompi salen de `appsettings.json` con valores de desarrollo. Fuera de tu máquina hay que reemplazarlas:

| Variable | Uso |
|---|---|
| `Auth__Secret` | Firma del JWT. Mínimo 32 caracteres |
| `DataProtection__Key` | 32 bytes en base64. Cifra las llaves de Wompi y el secreto de los webhooks |
| `ConnectionStrings__Orders` | Cadena de SQLite |
| `PublicBaseUrl` | Prefijo de la URL de checkout de sandbox |

No subas llaves reales de Wompi al repositorio. El demo público se recorre entero en sandbox.

## Recorrido mínimo

```sh
curl -s -X POST http://localhost:5080/account/register \
  -H 'content-type: application/json' \
  -d '{"name":"Tienda demo","email":"tienda@example.com","password":"clave-demo-1"}'

curl -s -X POST http://localhost:5080/account/login \
  -H 'content-type: application/json' \
  -d '{"email":"tienda@example.com","password":"clave-demo-1"}'
```

Con el JWT:

```sh
curl -s -X POST http://localhost:5080/account/api-keys \
  -H "authorization: Bearer <jwt>" \
  -H 'content-type: application/json' \
  -d '{"name":"local","environment":"sandbox"}'
```

Con la API key `sk_test_...`:

```sh
curl -s -X POST http://localhost:5080/v1/customers \
  -H "authorization: Bearer <api_key>" \
  -H 'content-type: application/json' \
  -d '{"name":"Ana","email":"ana@correo.com"}'

curl -s -X POST http://localhost:5080/v1/orders \
  -H "authorization: Bearer <api_key>" \
  -H 'content-type: application/json' \
  -d '{"customer_id":"<id>","lines":[{"name":"Audifonos","quantity":1,"unit_amount":120000}]}'

curl -s -X POST http://localhost:5080/v1/orders/<id>/payments \
  -H "authorization: Bearer <api_key>" \
  -H 'content-type: application/json' \
  -d '{"sandbox_result":"approved"}'
```

El pedido queda en `paid`. Si antes configuraste `PUT /v1/webhook` con una URL, sale un `POST` con el encabezado `X-Orders-Signature` en la forma `t=<unix>,v1=<hmac sha256 del cuerpo>`. La firma es el HMAC de `{timestamp}.{cuerpo}` usando el secreto que se devolvió una sola vez.

Para producción, con el JWT: `PUT /account/integrations/wompi` y después `POST /account/integrations/wompi/activate`. A partir de ahí la llave `sk_live_` pide el enlace a Wompi. El resultado real entra por `POST /webhooks/wompi`.

## Layout

| Proyecto | Qué es |
|---|---|
| `Orders.Domain` | Pedido, pago, comercio y las reglas de estado |
| `Orders.Application` | Casos de uso y los puertos hacia afuera |
| `Orders.Infrastructure` | SQLite, cifrado, HTTP a Wompi y envío de webhooks |
| `Orders.Api` | HTTP, Swagger, API keys y el límite de consumo |
| `Orders.Tests` | El flujo de sandbox y el de Wompi con un cliente falso |

## Fuera de esta versión

Una sola pasarela, Wompi, detrás de `IWompiClient`. No hay panel para el comprador. No se cobra por usar la API. El webhook de salida se intenta tres veces en la misma petición que cambia el pedido. Un proceso aparte de reintentos queda para cuando esto se esté operando.
