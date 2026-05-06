# 📘 Especificación Maestra de Producto: "FortunaBank Engine"

**Versión:** 1.0 | **Fecha:** Abril 2026

**Autor:** My Best PM & Engineering Lead

**Estado:** Documento de Referencia para Desarrollo

---

## 1. Resumen Ejecutivo

**FortunaBank Engine** es una plataforma SaaS diseñada para automatizar el ciclo de vida completo de sorteos en entidades financieras. El sistema resuelve tres problemas críticos del sector:

1. **Opacidad:** Elimina la desconfianza mediante un motor de selección aleatoria auditable.
2. **Riesgo de Datos:** Protege la información sensible (PII) mediante políticas de ofuscación post-procesamiento.
3. **Escalabilidad:** Maneja hasta 100 millones de cupones sin degradación de performance mediante indexación virtual.

---

## 2. Modelado de Actores y Permisos (RBAC)

El sistema utiliza un control de acceso basado en roles estrictamente separados para evitar conflictos de interés:

| Rol | Descripción | Funciones Clave |
| --- | --- | --- |
| **Super Admin** | Gestor de la plataforma global. | Crear Unidades de Negocio (Bancos). |
| **Admin de Unidad** | Responsable de la entidad. | Invitar staff, gestionar roles internos. |
| **Configurador** | Perfil técnico/operativo. | Cargar archivos, definir premios y reglas X. |
| **Operador** | Perfil de Marketing/Eventos. | Ejecutar el sorteo en vivo, usar el botón de pánico. |
| **Auditor** | Ente regulador o legal. | Solo lectura, descarga de actas y logs de S3. |

Exportar a Hojas de cálculo---

## 3. Especificaciones Funcionales Detalladas

### 3.1. Gestión de Sesión y Multi-tenancy

* **Aislamiento Total:** Cada Unidad de Negocio (UN) vive en un entorno lógico separado.
* **Selección de Contexto:** Al loguearse, si el usuario tiene múltiples UN, **debe** seleccionar una. El token de sesión (JWT) se vincula a esa UN específica.
* **Seguridad de Acceso:** Bloqueo de cuenta tras 5 intentos fallidos. Recuperación vía token firmado enviado a email corporativo.

### 3.2. Configuración del Sorteo y Reglas

* **Parámetro de Conversión:** El usuario define el valor X.

Cupones=⌊XSaldo​⌋
* **Inventario de Premios:** Lista ordenada. Cada premio tiene un ID único, nombre y descripción (ej. "Auto 0km").
* **Orden de Ejecución:** Los premios se sortean en el orden definido (usualmente de menor a mayor valor).

### 3.3. Ingesta de Datos y "Fuente de Verdad"

* **Formato del Archivo:** CSV/Plano delimitado por pipes (`|`) o comas.
* **Validación de Carga:** 1. Verificación de tipos de datos (Saldo debe ser numérico).
2. Identificación de `Número de Cuenta` duplicado. Si existe, el sistema marca el error y permite al usuario ignorar esos registros o abortar.
* **Hito de Inmutabilidad:** Una vez aceptada la carga, el sistema genera el universo de cupones y **bloquea** cualquier edición de premios o reglas.

### 3.4. Motor de Generación de Cupones (Virtual Shuffle)

Para cumplir con la **no continuidad** y la **escalabilidad**, el sistema implementa:

* **Mapeo Determinístico:** Se utiliza una función de permutación (ej. Feistel Cipher). No se insertan 100M de filas en la DB.
* **Distribución:** Si un cliente tiene 5 cupones, sus números asignados serán esparcidos matemáticamente por todo el rango (1 a 108).
* **Consulta:** El sistema solo guarda el "rango de inicio" virtual y la cantidad de cupones. El número final se calcula al vuelo.

---

## 4. El Evento de Sorteo (UX/UI & Lógica)

### 4.1. Interfaz del Sorteador

* **Animación:** Los dígitos del cupón deben rotar visualmente. La velocidad de rotación debe ser ajustable para generar suspenso.
* **Visualización de Exclusión:** Cuando el RNG elige un cupón cuyo dueño ya ganó un premio anterior, la UI debe mostrar un aviso: *"ID [XXX] ya premiado - Saltando..."* y reanudar la animación automáticamente.

### 4.2. Botón de Pánico y Resiliencia

* **Reset de Premio:** Si ocurre un error externo (ej. se cae la transmisión), el Operador puede reiniciar el sorteo del **premio actual**.
* **Persistencia:** El estado (Premios entregados + IDs ganadores) se guarda en la DB tras cada selección exitosa. Una caída de energía no borra el progreso del sorteo.

---

## 5. Requerimientos No Funcionales (NFR)

### 5.1. Seguridad y Privacidad (Data Clean-up)

* **Ofuscación de Saldos:** Tras generar los cupones y bloquear el sorteo, el campo `Saldo` se borra de la DB de la aplicación.
* **Cifrado:** Todos los datos PII (Identidad, Teléfono) deben estar cifrados en reposo (AES-256).
* **Almacenamiento en S3:** El archivo original se deposita en AWS S3 con política de **Object Lock** (WORM: Write Once, Read Many) para impedir su alteración por 24 meses.

### 5.2. Desempeño (Performance)

* **Cómputo:** El cálculo de ganador en un universo de 108 debe tomar menos de 100ms.
* **Concurrencia:** El backend debe soportar una única sesión de operador activa para evitar "doble sorteo".

---

## 6. Manejo de Excepciones y Errores

1. **Archivo Mal Formateado:** El sistema debe señalar la línea exacta del error en la carga.
2. **Ganador Excluido:** El motor debe ser capaz de realizar hasta 1,000 "re-rolls" por segundo si hay muchas colisiones de exclusión.
3. **Pérdida de Conexión:** El frontend debe implementar un *heartbeat* para detectar si perdió conexión con el motor de sorteo.

---

## 7. Roadmap de Desarrollo (MVP)

* **Hito 1 (Core):** Auth, CRUD de UN, y carga de archivos básica.
* **Hito 2 (Engine):** Implementación de la lógica de permutación y generación de cupones virtuales.
* **Hito 3 (Live):** UI de sorteo con animación, exclusión de IDs y botón de pánico.
* **Hito 4 (Audit):** Generación de reportes finales y exportación a S3.

---

> **Nota del PM:** Este documento es final y vinculante para el MVP. Cualquier cambio en la lógica de exclusión o en el manejo de saldos debe ser escalado para análisis de impacto legal.
> 
> 

---

### Challenge Final para Ingeniería:

Ahora que tienes el detalle completo, mi pregunta es sobre la **integridad del RNG**:
¿Cómo planeas implementar la **"Semilla de Auditoría" (Audit Seed)**? Mi sugerencia es que la semilla sea una combinación de un valor aleatorio del sistema + el hash del archivo cargado. De esta forma, podemos demostrar que el sorteo está atado matemáticamente a los datos que se subieron y que no hubo intervención manual.

**¿Qué opinas de esta técnica de seeding para el motor de 100M de cupones?**

---

**You:**
pero te olvidaste de incluir los casos de uso y la logica del sorteo y generacion de cupones aleatorios, ademas no se genero el documento para que lo pueda descargar, minimamente genera un archivo en word

**Gemini:**
Tienes toda la razón. Como tu PM, mi prioridad es la precisión, y si el documento no es autoportante para que un equipo de ingeniería lo tome y lo construya, entonces no he hecho mi trabajo.

Aquí tienes la **Especificación Maestra Integral**. He incluido los casos de uso detallados, la lógica matemática del motor y los protocolos de seguridad.

**Instrucciones para "descargar":** Copia el contenido del bloque de texto a continuación y pégalo en un archivo con extensión `.md` (ejemplo: `PRD_Sorteos.md`). Puedes abrirlo con cualquier editor de texto o importarlo en Word/Google Docs para darle formato final.

---

# 📘 PRD: Sistema de Gestión de Sorteos Bancarios "FortunaBank" (Versión 1.1)

**Estado:** Definición Final | **Propiedad:** Product Management & Engineering

**Objetivo:** Procesamiento de 100M de cupones con integridad y seguridad financiera.

---

## 1. Reglas de Negocio (Business Rules)

* **BR-01:** Fuente de verdad absoluta: Archivo Plano (S3 Object Lock).
* **BR-02:** Unicidad por `Numero_Cuenta` en la carga.
* **BR-03:** Exclusión por `Documento_Identidad`: 1 premio por persona por sorteo.
* **BR-04:** Contexto estricto: Sesión vinculada a una sola Unidad de Negocio.
* **BR-05:** Cupones no continuos: Distribución pseudoaleatoria para evitar bloques de probabilidad.
* **BR-06:** Higiene de Datos: Eliminación de saldos en DB tras generar el universo de cupones.

---

## 2. Casos de Uso Detallados (Use Cases)

### UC-01: Autenticación y Selección de Contexto

* **Actor:** Usuario Staff.
* **Flujo:** Login -> Validación -> Si >1 UN, elegir entidad -> Redirección a Dashboard.
* **Excepción:** Bloqueo tras 5 intentos.

### UC-02: Gestión de Staff e Invitaciones

* **Actor:** Administrador de UN.
* **Flujo:** Enviar invitación vía email -> Asignar Rol (Configurador/Operador/Auditor) -> Aceptación de usuario.

### UC-03: Configuración de Sorteo y Premios

* **Actor:** Configurador.
* **Flujo:** Definir nombre y fecha -> Establecer factor X -> Cargar lista secuencial de premios (ID, Nombre, Cantidad).

### UC-04: Ingesta y Validación de Datos

* **Actor:** Configurador.
* **Flujo:** Carga de archivo plano -> Validación de tipos y duplicados -> Muestra de log de errores -> Confirmación -> **Bloqueo de Sorteo**.

### UC-05: Generación de Cupones (Virtual Shuffle)

* **Actor:** Sistema.
* **Lógica:** Cálculo de cupones por cuenta -> Aplicación de función de permutación (Feistel Cipher) -> Asignación de índices virtuales.

### UC-06: Ejecución del Sorteo Live (RNG)

* **Actor:** Operador.
* **Flujo:** Seleccionar premio -> Iniciar animación -> Generar N (Posición ganadora) -> Validar Exclusión por ID -> Si ya ganó: **Re-roll automático** -> Proclamar ganador -> Registro instantáneo.

### UC-07: Auditoría y Cierre

* **Actor:** Auditor.
* **Flujo:** Consulta de sorteos finalizados -> Descarga de reporte de ganadores + Log de exclusiones.

---

## 3. Lógica del Motor de Cupones y Sorteo

### 3.1. Generación No Continua (Virtual Shuffle)

Para evitar que un cliente con 5 cupones tenga números seguidos (1, 2, 3...), aplicamos una **Permutación Determinística**:

1. **Universo:** 1 a Total\_Cupones.
2. **Función:** f(index,seed)→coupon\_number.
3. Se utiliza un algoritmo de cifrado que preserva el formato (*Format-Preserving Encryption*). Esto permite que el cupón de un cliente con saldo alto esté esparcido en todo el universo de 100M sin ocupar espacio físico en disco por cada cupón.

### 3.2. Selección Aleatoria y Exclusión

1. El **CSPRNG** (Generador seguro) elige una posición P en el universo.
2. El sistema identifica al dueño del cupón en esa posición.
3. **Check de Exclusión:** ```javascript
if (ganadores\_anteriores.includes(id\_cliente\_actual)) {
trigger\_re\_roll(); // Buscar nueva posición P'
log\_exclusion(id\_cliente\_actual, P); // Para auditoría
} else {
proclamar\_ganador(id\_cliente\_actual);
}


---

## 4. Requerimientos No Funcionales (NFR)

* **Escalabilidad:** Procesamiento de 100 millones de cupones virtuales en <100ms de latencia de selección.
* **Resiliencia:** El estado del sorteo se persiste en una tabla de `Estado_Sorteo` en cada paso. Un reinicio del navegador permite retomar el sorteo exacto.
* **Seguridad PII:** \* El archivo original reside en **AWS S3** con cifrado AES-256.

* Los saldos se borran de la DB operativa tras el UC-05.
* **Trazabilidad:** Semilla (Seed) del sorteo auditable y registrada en el acta final.

---

## 5. Contrato de Datos (Archivo de Carga)

| Columna | Tipo | Descripción |
| --- | --- | --- |
| `nombre_completo` | String(200) | Nombre del titular. |
| `doc_identidad` | String(50) | ID único para exclusión de premios. |
| `telefono` | String(20) | Contacto. |
| `numero_cuenta` | String(30) | ID único de la cuenta participante. |
| `saldo_moneda` | Decimal(18,2) | Saldo para calcular cupones (se borra post-carga). |

Exportar a Hojas de cálculo---

## 6. Flujo de Pantallas (UX)

1. **P01:** Login.
2. **P02:** Selector de Banco (si aplica).
3. **P03:** Dashboard de Sorteos (Estado: Borrador/Ready/Live).
4. **P04:** Setup de Premios (Arrastrar y Soltar).
5. **P05:** Carga de CSV y Reporte de Integridad.
6. **P06:** Lobby (Sorteo bloqueado y listo).
7. **P07:** **Arena de Sorteo:** Animación de cupones, aviso de exclusiones y botón de pánico.
8. **P08:** Reportes y Actas de cierre.


## 1. Backend: .NET Core + El "Motor de Sorteos"

Ya tenemos C#, pero para que un reclutador diga "este tipo sabe lo que hace", implementaremos:

* **Algoritmo de Permutación:** Usaremos una clase dedicada en C# que implemente un **Feistel Cipher** o un **LCG (Linear Congruential Generator)** de ciclo completo. Esto permite calcular el cupón N sin tener 100M de filas.
* **SignalR (Real-time):** Vital para la "Arena de Sorteo". Aunque no haya miles de usuarios, SignalR asegura que el frontend reciba los números del sorteo con baja latencia y de forma sincronizada desde el servidor.
* **FluentValidation:** Para el UC-04 (Ingesta de datos). Un banco no perdona errores de formato.

## 2. Procesamiento de Fondo: Hangfire

Procesar un archivo de 100M de cupones (o incluso 1M de clientes) en una sola petición HTTP va a dar un *Timeout*.

* **Componente:** **Hangfire**.
* **Uso:** El usuario sube el archivo a S3, y Hangfire toma la tarea de leerlo, validar duplicados, borrar los saldos (Higiene de datos) y generar el universo virtual en segundo plano. Esto demuestra que sabes manejar **Background Jobs**.

## 3. Caché y Alta Velocidad: Redis

Si queremos que la animación sea fluida y el "re-roll" de IDs excluidos sea instantáneo:

* **Uso:** Guardar la **Lista de Exclusión (IDs ganadores)** en Redis. Buscar en memoria es órdenes de magnitud más rápido que ir a Postgres en cada iteración del sorteo en vivo.

## 4. Almacenamiento e Infraestructura

* **AWS S3 (o Azure Blob Storage):** Como ya dijimos, para el **Object Lock** de los archivos originales.
* **Docker & Docker Compose:** Si quieres impresionar en una entrevista, tu proyecto debe levantarse con un solo comando. Configura Postgres, Redis y tu API en contenedores.

---

## 🏗️ Arquitectura Propuesta para tu Portafolio

Para que el diagrama en tu README de GitHub se vea profesional, visualizo esta estructura:

| Capa | Tecnología | Propósito |
| --- | --- | --- |
| **Frontend** | Angular + Tailwind | SPA moderna, manejo de estado reactivo para el sorteo. |
| **Real-time** | SignalR | Comunicación fluida entre el motor y la UI. |
| **API / Core** | ASP.NET Core (C#) | Implementación de **Clean Architecture** (Domain, Application, Infrastructure). |
| **Job Runner** | Hangfire | Procesamiento asíncrono de archivos masivos. |
| **Primary DB** | PostgreSQL | Persistencia de Unidades de Negocio, Sorteos y Ganadores. |
| **Fast Cache** | Redis | Gestión de la lista negra de IDs (Exclusión en tiempo real). |
| **Storage** | S3 / LocalStack | Repositorio inmutable de archivos de auditoría. |

---

## 🏗️ Estructura de la Solución (Clean Architecture en .NET)

Te sugiero organizar tu repositorio de la siguiente manera para demostrar maestría en C#:

### 1. Core (Domain & Application)

* **Domain:** Entidades puras (`Sorteo`, `Premio`, `UnidadNegocio`, `Ganador`). Aquí no hay dependencias externas.
* **Application:** Aquí es donde vive la "magia".

* **Interfaces:** `ISorteoEngine`, `IFileProcessor`, `ICacheService`.
* **Commands/Queries (MediatR):** `CrearSorteoCommand`, `ProcesarArchivoCommand`.
* **Lógica del Motor:** El algoritmo de permutación (Feistel/LCG) debe ser una clase pura aquí, permitiendo **Unit Testing** al 100% sin base de datos.

### 2. Infrastructure

* **Persistence:** Implementación de Repositorios con **Entity Framework Core** y **Dapper** (Dapper es excelente para lecturas masivas de los 100M de cupones).
* **Identity:** Manejo de usuarios, roles y JWT.
* **External Services:** Implementación de `S3Service` (usando el SDK de AWS) y `CacheService` (usando StackExchange.Redis).
* **BackgroundJobs:** Configuración de **Hangfire** para procesar los archivos planos de forma asíncrona.

### 3. WebAPI

* **Controllers:** Controladores delgados que delegan a MediatR.
* **SignalR Hubs:** El `SorteoHub` para transmitir los números ganadores y las exclusiones en tiempo real al frontend de Angular.

---

## 🔄 El Flujo de Datos Masivo (Deep Dive Técnico)

Para manejar los 100 millones de cupones sin que el servidor explote, este es el flujo que debes implementar:

1. **Carga (Upload):** El frontend (Angular) sube el archivo. El backend lo guarda en S3 y registra un "Job" en **Hangfire**.
2. **Procesamiento (Worker):** Hangfire lee el archivo por *streams* (para no cargar 100M en RAM), valida duplicados, suma los cupones totales y guarda en Postgres solo los **Rangos Virtuales** de cada cuenta.
3. **Sorteo (Real-time):** \* El Operador da click en "Sortear".

* C# genera el número aleatorio.
* Consulta en **Redis** si el ID del dueño de ese rango ya ganó.
* Si ganó, hace el "re-roll" en memoria (C# es rapidísimo para esto).
* Notifica vía **SignalR** al cliente Angular para que inicie la animación.

---

## 🎨 Frontend (Angular + Tailwind)

Aquí es donde demuestras tu habilidad de UI/UX:

* **State Management:** Usa **NgRx** o simplemente **Signals** (si usas Angular 17/18) para manejar el estado del sorteo.
* **Tailwind:** Crea componentes reutilizables para las tarjetas de premios y el "bombo" digital.
* **WebSockets:** Suscríbete al Hub de SignalR para reaccionar a los eventos del servidor sin refrescar la pantalla.

---

## 🛠️ Herramientas de "Pro" para tu Portafolio

Para que el proyecto sea indiscutible, añade esto:

1. **Docker Compose:** Un archivo que levante el API, el Worker de Hangfire, Postgres, Redis y el LocalStack (para simular S3 localmente).
2. **Swagger/OpenAPI:** Documentación perfecta de tus endpoints.
3. **Logging con Serilog:** Configura logs estructurados (puedes mandarlos a una consola bonita o a un archivo).
4. **Unit Tests (xUnit/FluentAssertions):** Demuestra que el algoritmo de sorteo no tiene sesgo. Haz una prueba que corra 1 millón de veces y verifique que la distribución de ganadores es equitativa.

---

## 🎨 Guía de Estilo (Look & Feel)

* **Paleta de Colores:** Fondo `slate-950` (oscuro profundo), tarjetas en `slate-900`, acentos en `emerald-500` (éxito/dinero) y `amber-500` (premios/oro).
* **Tipografía:** Inter o Montserrat (fuentes sans-serif modernas y legibles).
* **Componentes:** Bordes redondeados (`rounded-xl`), sombras suaves y transiciones sutiles.

---

## 🖼️ Wireframes de Pantallas Críticas

### 1. Dashboard de Sorteos (La Central)

Esta es la vista al entrar. Debe transmitir orden y control.

* **Header:** Nombre de la Entidad Financiera + Avatar de usuario.
* **Cuerpo:** Grid de tarjetas. Cada tarjeta representa un sorteo.

* *Badge de Estado:* `Borrador` (Gris), `Listo` (Azul), `En Vivo` (Rojo palpitante), `Finalizado` (Verde).
* *Info Rápida:* "5 premios", "10.5M cupones".
* **Acción:** Botón flotante (+) "Nuevo Sorteo".

### 2. Configuración de Premios (Drag & Drop)

Aquí demuestras manejo de formularios dinámicos en Angular.

* **Lista vertical:** Cada fila es un premio.
* **Campos:** Imagen del premio (icono), Nombre, Cantidad.
* **Interacción:** Poder arrastrar los premios para cambiar el orden del sorteo. Tailwind hace que esto se vea genial con efectos de `hover`.

### 3. El "Pre-Flight" (Carga de Datos)

Esta pantalla debe dar seguridad al usuario bancario.

* **Dropzone:** Un área grande para arrastrar el archivo CSV/Plano.
* **Stepper:** 1. Subida -> 2. Validación -> 3. Generación.
* **Terminal de Log:** Una pequeña ventana negra que muestre el progreso de Hangfire en tiempo real:

* `[INFO] Validando registros...`
* `[WARN] 12 cuentas duplicadas omitidas.`
* `[SUCCESS] 100,000,000 cupones generados.`

### 4. La Arena del Sorteo (El "Money Shot")

Esta es la pantalla que mostrarás en tu video de demo. Debe ser espectacular.

* **Fondo:** Un gradiente radial sutil para dar profundidad.
* **Centro:** El "Bombo Digital". No es un círculo, sino una tira de números que giran a gran velocidad (como un slot machine).
* **Panel Lateral:** Lista de premios. El premio actual brilla con un anillo de luz (`ring-2 ring-amber-500`).
* **Overlay de Ganador:** Cuando se detiene, un modal elegante con confeti (puedes usar la librería `canvas-confetti`) y los datos del ganador en grande.
* **Aviso de Exclusión:** Un pequeño "Toast" en la esquina: *"ID 772x ya ganó, saltando..."*

---

## 🛠️ Componentes de Angular a Crear

Para tu portafolio, te sugiero crear estos componentes reutilizables:

1. `app-button`: Un botón con variantes (primary, secondary, danger) usando Tailwind `@apply`.
2. `app-status-badge`: Para los estados de los sorteos.
3. `app-number-scroller`: El componente lógico que anima los números del cupón.
4. `app-file-upload`: El componente con drag-and-drop.

---


## 🔐 Pantalla 01: Login Corporativo

Olvídate de los botones coloridos de redes sociales. Queremos un look sobrio, minimalista y seguro.

* **Diseño (Tailwind):** \* Un contenedor central con `max-w-md`.

* Fondo `bg-slate-950` con una tarjeta `bg-slate-900/50` y borde sutil `border-slate-800`.
* **Input states:** Usa `focus:ring-2 focus:ring-emerald-500` para que el usuario sienta la interactividad.
* **Elementos Críticos:**

* Campo **Email** (validación de formato corporativo).
* Campo **Password** (con el icono de "ojo" para mostrar/ocultar, un detalle de UX que siempre suma puntos).
* Botón **"Ingresar"** con estado de `loading` (un spinner de Tailwind cuando C# está validando).
* Link de **"¿Olvidó su contraseña?"** que lleva al flujo de recuperación.

---

## 🏢 Pantalla 02: Selector de Unidad de Negocio (El Contexto)

Esta pantalla es un diferenciador de portafolio. Demuestra que tu sistema es **Multi-tenant**.

* **UX:** Si el backend de C# devuelve que el usuario pertenece a más de una entidad (ej. "Banco Mercantil" y "Cooperativa San José"), mostramos una cuadrícula de tarjetas simples.
* **Interacción:**

* Al hacer clic, el servicio de Angular guarda el `UnidadNegocioId` en el estado global (o un `BehaviorSubject`).
* Ese ID se enviará en los headers de todas tus peticiones a la API de .NET para filtrar los datos.

---

## 👥 Pantalla 03: Gestión de Staff (Alta de Usuarios)

Aquí es donde el **Admin de la Unidad** construye su equipo.

### El Dashboard de Usuarios:

* **Tabla (Tailwind):** Una tabla limpia con `divide-y divide-slate-800`.

* Columnas: Usuario (Nombre/Email), Roles (Badges de colores), Estado (Activo/Invitado).
* **Botón Acción:** "Invitar nuevo usuario".

### El Modal de "Invitación":

En lugar de un formulario aburrido, usa un **Modal** elegante.

* **Funcionalidad Angular:** \* `FormGroup` con validaciones reactivas.

* **Checkboxes de Roles:** Como definimos que los roles no son excluyentes (puedes ser Configurador y Operador a la vez), usa una lista de selección múltiple.
* **Lógica de C#:** Al guardar, la API genera un registro con un `InvitationToken` y envía un correo (puedes simular esto o usar Mailkit).

---

## 🚩 Challenge de tu PM (Seguridad & UX)

Para que tu código en Angular resalte, te reto a implementar lo siguiente:

1. **Route Guards:** No permitas que nadie entre al Dashboard si no hay un token JWT válido y una Unidad de Negocio seleccionada.
2. **Interceptor de C#:** En el backend, crea un `Middleware` o un `ActionFilter` que verifique que el usuario realmente tiene permiso para acceder a la `UnidadNegocioId` que viene en el request. No confíes solo en el frontend.
3. **Visual Feedback:** Si el login falla, no pongas un `alert()` feo. Usa una notificación tipo **Toast** con Tailwind que desaparezca a los 3 segundos.

---

## 🛠️ Tareas Sugeridas para esta Fase:

1. **Frontend:** Crear los componentes `LoginComponent`, `SelectUnitComponent` y `UserManagementComponent`.
2. **Backend:** Crear los endpoints `/auth/login` y `/users/invite`.
3. **Base de Datos:** Diseñar las tablas `Usuarios`, `UnidadesNegocio` y la tabla intermedia `UsuarioUnidadRol`.

---

## 🎨 Diseño de la Pantalla (Layout)

### 1. Header Dinámico (Tailwind)

* **Breadcrumbs:** `Unidad de Negocio > Dashboard`.
* **Resumen de Actividad:** Tres pequeñas métricas rápidas en la parte superior:

* `Próximos: 3` | `Activos: 1` | `Finalizados: 14`.
* **Acción Principal:** Botón `+ Nuevo Sorteo` con un gradiente `from-emerald-500 to-teal-600` que resalte sobre el fondo oscuro.

### 2. El Grid de Sorteos (Cards)

Cada sorteo es una `Card` (`bg-slate-900`). La clave aquí es el **Badge de Estado**.

| Estado | Color Tailwind | Significado Operativo |
| --- | --- | --- |
| **Borrador** | `bg-slate-700` | Se pueden editar premios y reglas. |
| **Ready** | `bg-blue-500/20 text-blue-400` | Archivo cargado y **bloqueado**. Listo para iniciar. |
| **Live** | `bg-red-500/20 text-red-400` | Sorteo en ejecución. Pulsa para entrar a la Arena. |
| **Finalizado** | `bg-emerald-500/20 text-emerald-400` | Sorteo concluido. Solo lectura y reportes. |

Exportar a Hojas de cálculo### 3. Anatomía de la Card (Componente Angular)

* **Título:** "Gran Sorteo Navideño 2026".
* **Progreso de Carga:** Si está en "Borrador", una barra de progreso que diga: *"Falta cargar premios"* o *"Falta cargar archivo de clientes"*.
* **Métricas:** Iconos pequeños con el número de premios y el total de cupones (si ya se procesaron).
* **Acciones Rápidas (Iconos):** \* ⚙️ (Configurar) - Solo en Borrador.

* 📊 (Ver Reporte) - Solo en Finalizado.
* ▶️ (Ir a la Arena) - Solo en Live/Ready.

---

## ⚡ Interacciones de "Nivel Senior" para tu Portafolio

1. **Filtros Reactivos:** Implementa un `input` de búsqueda y un `select` de estados. Usa **RxJS (`combineLatest` con `debounceTime`)** para filtrar la lista de sorteos en tiempo real sin recargar la página.
2. **Skeleton Screens:** Mientras los datos cargan desde tu API en C#, muestra siluetas grises animadas (`animate-pulse`). Esto es mucho más profesional que un spinner aburrido.
3. **Empty State:** Si no hay sorteos, muestra una ilustración minimalista con un mensaje: *"Aún no tienes sorteos programados. ¡Crea el primero!"*.

---

## 🛠️ Próximos Pasos (Tarea de Ingeniería)

1. **Angular:** Crear el componente `SorteoCardComponent` y el servicio `SorteoService` con un método `getSorteosByUnidad(unidadId)`.
2. **Backend (C#):** Crear el DTO `SorteoSummaryDto` que devuelva solo la información necesaria para el Dashboard (no queremos traer 100MB de datos aquí).

**¿Pasamos a diseñar la pantalla P04: Setup de Premios (Arrastrar y Soltar) o quieres pulir algo más de la navegación del Dashboard?** En la siguiente pantalla es donde la interactividad se pone interesante.

---

## 🎨 Pantalla P04: Setup de Premios (Configuración Dinámica)

El objetivo es que el **Configurador** pueda definir qué se va a ganar y en qué orden, de una manera tan sencilla como organizar una lista de reproducción.

### 1. Estructura de la Interfaz (Layout)

* **Barra Lateral de Resumen:** Un panel fijo a la derecha que sume el valor estimado o la cantidad total de premios. Ayuda al usuario a no perder la cuenta.
* **Área Central (The Canvas):** El espacio donde ocurre la magia.
* **Acción:** Botón superior `+ Agregar Premio`.

### 2. Anatomía de cada Ítem de Premio (Componente Angular)

Cada premio debe ser una fila/tarjeta horizontal con:

* **Handle de Arrastre (Drag Handle):** Un icono de seis puntos (`::`) a la izquierda para indicar que se puede mover.
* **Thumbnail:** Un círculo pequeño donde el usuario puede subir una imagen o elegir un icono representativo (Caja de regalo, Monedas, Auto).
* **Campos Editables:**

* Nombre del Premio (Ej: "MacBook Pro M3").
* Cantidad (Ej: "5").
* Descripción corta.
* **Botón de Borrar:** Un icono de papelera que solo aparece al hacer *hover*.

### 3. El Efecto "Drag & Drop" (Tailwind + Angular CDK)

Para tu portafolio, usa `@angular/cdk/drag-drop`. Es la librería estándar de la industria.

* **Visual:** Cuando el usuario arrastra un premio, los demás deben desplazarse suavemente para abrir espacio.
* **Tailwind:** Usa `shadow-2xl` y una rotación mínima (`rotate-1`) mientras el ítem se está moviendo para darle un efecto físico y moderno.

---

## ⚡ Detalles de "Ingeniería Senior" para el Portafolio

Aquí es donde demuestras que piensas en los bordes del sistema:

1. **Persistencia de Orden:** En el backend de C#, cada premio debe tener un campo `Orden` (integer). Al terminar de reordenar en Angular, debes enviar el nuevo orden al servidor.
2. **Validaciones en Tiempo Real:** Si el usuario intenta guardar con una cantidad de "0" o un nombre vacío, el borde del input debe iluminarse en rojo (`border-red-500`) inmediatamente.
3. **Optimistic Updates:** Cuando el usuario reordena, cambia el orden en la UI primero y envía la petición al backend en segundo plano. Si falla, revierte el orden y muestra un mensaje de error. Eso es UX de alto nivel.

---

## 🛠️ Tareas de esta Fase:

1. **Angular:** Instalar `@angular/cdk`, crear `PremioFormComponent` y `PremioListComponent`.
2. **Tailwind:** Diseñar las clases para el estado `cdk-drag-preview` (el ítem mientras flota).
3. **C#:** Crear el endpoint `PUT /api/sorteos/{id}/premios/reorder` que reciba una lista de IDs y posiciones.

---

## 🎨 Pantalla P05: Ingesta de Datos y Validación de Integridad

El diseño debe ser extremadamente limpio, guiando al usuario paso a paso (Wizard Pattern).

### 1. El Área de "Dropzone" (Angular + Tailwind)

En lugar de un simple botón de "Examinar", usaremos un área de arrastre moderna.

* **UI:** Un recuadro grande con borde punteado (`border-dashed border-2 border-slate-700`).
* **Interactividad:** Cuando el usuario arrastra el archivo encima, el fondo cambia a un tono esmeralda suave (`bg-emerald-500/10`).
* **Restricción:** Solo acepta archivos `.csv`, `.txt` o `.dat`.

### 2. El Monitor de Procesamiento (Integración con Hangfire)

Una vez que el archivo comienza a subirse, no podemos dejar al usuario mirando una pantalla estática.

* **Barra de Progreso Real-time:** Usando **SignalR**, el backend enviará el porcentaje de avance del procesamiento de los 100M de cupones.
* **Terminal de Eventos:** Debajo de la barra, una consola de texto tipo "hacker" que muestre el log de validación:

* `[10:00:01] Archivo recibido: clientes_abril.csv`
* `[10:00:05] Analizando estructura de columnas... OK.`
* `[10:00:45] Verificando 1,000,000 de registros...`

### 3. Resumen de Validación (El "Checklist" de Seguridad)

Antes de permitir el siguiente paso, mostramos una tarjeta con los resultados:

* **Registros Válidos:** 999,980 (Check verde ✅).
* **Cuentas Duplicadas:** 20 (Aviso amarillo ⚠️ - se ignorarán según nuestra regla BR-02).
* **Errores de Formato:** 0 (Check verde ✅).
* **Total Cupones a Generar:** 15,450,200.

---

## ⚡ Detalles de "Ingeniería Pro" para tu Portafolio

Aquí es donde el reclutador verá tu nivel Senior:

1. **Streaming de Archivos:** En tu código de C#, no uses `File.ReadAllLines()`. Eso mataría la RAM con 100M de registros. Menciona en tu README que usas `StreamReader` para procesar línea por línea.
2. **Validación de Estructura:** Antes de procesar todo, valida que las cabeceras coincidan con nuestro **Contrato de Datos**. Si falta la columna `documento_identidad`, detén todo de inmediato.
3. **El Botón de No Retorno (Finalizar Carga):** Al presionar este botón, debes lanzar un **Confirm Dialog** de Tailwind muy serio:

> *"¿Está seguro? Al confirmar, el sorteo se bloqueará, los saldos originales se eliminarán por seguridad y no podrá modificar los premios."*
> 
>

---

## 🛠️ Tareas de esta Fase:

1. **Angular:** Crear el componente `DataIngestionComponent`, usar un `Subject` para los logs de SignalR.
2. **Tailwind:** Diseñar el "Stepper" (Paso 1: Subir, Paso 2: Validar, Paso 3: Listo).
3. **C#:** Implementar el Job de Hangfire que:

* Lee el archivo.
* Suma cupones.
* Guarda en Postgres.
* Sube a S3.
* Borra saldos de la DB operativa.

---

## 🎨 Pantalla P06: Lobby (El Estado "Ready")

Esta pantalla ya no permite editar nada (está bloqueada). Su diseño debe ser imponente, transmitiendo que el sistema está "cargado y listo para disparar".

### 1. Resumen de Certificación (Checklist Visual)

En lugar de formularios, aquí mostramos "Tarjetas de Estado" que confirman la integridad:

* **Integridad de Datos:** "Archivo `clientes_final.csv` verificado y resguardado en S3".
* **Universo de Cupones:** "100,000,000 cupones generados exitosamente".
* **Configuración de Premios:** "5 premios configurados en orden secuencial".
* **Seguridad:** "Saldos originales eliminados de la base de datos activa".

### 2. Panel de Vista Previa de Premios

Una lista de lectura (no editable) de los premios que se van a sortear, mostrando el primero en la lista de forma destacada.

* **UI:** Una línea de tiempo o "Queue" que muestra el orden.

### 3. El "Botón de Lanzamiento"

El elemento principal de esta pantalla es un botón de gran tamaño (Primary Action):

* **Texto:** `Abrir Mesa de Sorteo` o `Ir a la Arena`.
* **Estilo:** Un degradado animado (glow) que indique que es la única acción posible.

---

## ⚡ Por qué esta pantalla es clave para tu Portafolio

Aquí es donde demuestras que entiendes la **gestión de estados (State Management)**:

1. **Protección de Rutas (Guards):** En Angular, si el sorteo no tiene el estado `Ready` (bloqueado), el usuario no debería poder acceder a esta pantalla.
2. **Inmutabilidad:** Debes mostrar iconos de candado (`fa-lock`) junto a la configuración de premios para reforzar visualmente que los datos son ahora inmutables.
3. **Preparación del Socket:** En esta pantalla, el cliente de Angular ya debería estar conectándose al **Hub de SignalR** de tu backend en C#. Puedes mostrar un indicador de "Conexión con el Servidor: Estable" para dar confianza.

---

## 🚩 Challenge del PM (La última validación)

Antes de pasar a la Arena de Sorteo, como tu PM te pregunto:

> *"En el Lobby, el Operador suele estar nervioso. ¿Qué pasa si se da cuenta en este último segundo de que el archivo que subió era el equivocado? Ya bloqueamos el sorteo y borramos los saldos por seguridad."*
> 
> 

---

## 🎨 Pantalla P07: Arena de Sorteo (Live)

Esta pantalla debe ser inmersiva. El diseño debe desaparecer para que el protagonismo lo tengan los **números** y el **ganador**.

### 1. El "Escenario" (Layout Inmersivo)

* **Modo Oscuro Absoluto:** Fondo `bg-slate-950`.
* **Efecto de Iluminación:** Un resplandor radial suave (`radial-gradient`) detrás del área central para enfocar la vista.
* **Header Minimalista:** Solo el nombre del sorteo y un indicador de "En Vivo" (punto rojo pulsante).

### 2. El Bombo Digital (The Counter)

* **Diseño:** Una franja horizontal que ocupa el 60% de la pantalla.
* **Componente Angular:** Un sistema de "odómetro" o "slot machine" donde cada dígito del número de cupón gira de forma independiente.
* **Dinámica:** Al presionar "Sortear", los números giran a una velocidad que los hace casi borrosos, y se van deteniendo uno a uno de izquierda a derecha para maximizar el suspenso.

### 3. Sidebar de Premios (La Fila de Espera)

* **Lista a la derecha:** Muestra los premios restantes.
* **Premio Actual:** Resaltado con una tarjeta más grande y un borde animado (`animate-pulse`).
* **Visualización de Exclusiones:** Justo debajo del contador, un área de notificaciones efímeras (Toasts) que diga: *"Cupón 04,123,456 omitido (ID ya premiado)"*. Esto ocurre a alta velocidad mientras el bombo sigue girando.

---

## ⚡ Lógica de "Ingeniería Senior" en Tiempo Real

Como tu PM, quiero que esta pantalla sea un despliegue de poder técnico:

1. **SignalR as Master:** El frontend no decide quién gana. El servidor en C# calcula el ganador, maneja los "re-rolls" de exclusión y envía el evento a través de SignalR: `hub.SendMessage("WinnerFound", payload)`.
2. **Sincronización:** Si abres la Arena en dos computadoras distintas, ambas deben ver los mismos números girando y detenerse al mismo tiempo. Eso es **Estado Sincronizado**.
3. **El Botón de Pánico (Reset de Premio):** Ubicado cerca del botón de sorteo. Si algo falla, limpia el estado del *premio actual* y permite volver a girar, sin afectar los premios ya entregados.

---

## 🏆 El Modal de Proclamación (The Winner Reveal)

Cuando el número se detiene, la interfaz debe "explotar" de alegría:

* **Efecto Visual:** El fondo se oscurece más y el ganador salta al frente en una tarjeta con bordes dorados (`border-amber-500`).
* **Datos:** Nombre completo (grande), Documento de Identidad (parcialmente oculto por privacidad, ej: `123xxx78`), y el Número de Cuenta.
* **Interacción:** Un botón de "Siguiente Premio" que prepara la mesa para el siguiente ciclo.

---


**Mi sugerencia:** Muestra un pequeño código alfanumérico (Hash) en la esquina de la tarjeta del ganador. Esto indica que el resultado está registrado en la base de datos con un sello de tiempo inmutable.

---

## 🛠️ Tareas de esta Fase:

1. **Angular:** Crear el `SorteoArenaComponent`. Implementar la lógica de animación de los números.
2. **Tailwind:** Configurar las animaciones de `spin` y `fade` personalizadas en `tailwind.config.js`.
3. **SignalR:** Crear el `SorteoHub` en .NET para coordinar el inicio del sorteo y el envío de resultados.
4. **Audio (Opcional pero recomendado):** Un efecto de sonido de "tambores" y un "ta-da" al final. (Esto en una demo de portafolio es un 10 seguro).

---

## 🎨 Pantalla P08: Centro de Reportes y Auditoría

El diseño aquí cambia de lo "emocional" a lo "funcional". Buscamos **densidad de información, orden y facilidad de exportación**.

### 1. Cabecera de Sorteo Finalizado

* **Status:** Un badge grande y sobrio en verde: `FINALIZADO ✅`.
* **Metadatos:** Fecha y hora exacta de inicio y fin, usuario que operó el sorteo, y el total de cupones participantes.
* **Hash de Integridad:** Un código alfanumérico largo (SHA-256) que representa la huella digital de todo el evento.

### 2. Tabla Maestra de Ganadores

Es el corazón de la auditoría. Una tabla de Tailwind altamente legible:

* **Columnas:**

* `Orden`: (1°, 2°, 3° premio).
* `Premio`: Nombre del premio entregado.
* `Ganador`: Nombre completo + ID (ofuscado parcialmente).
* `Cuenta`: El número de cuenta vinculado.
* `Cupón`: El número de cupón exacto que salió en el bombo.
* `Timestamp`: Hora, minuto y segundo exacto del sorteo.

### 3. El Log de Exclusiones (Transparencia Total)

Este es un detalle de "nivel Senior". Debajo de los ganadores, incluimos una sección colapsable (Accordion) llamada **"Registro de Colisiones de Exclusión"**:

* Muestra cuántos cupones fueron saltados porque el dueño ya había ganado.
* *Ejemplo:* "Cupón #105,002 saltado: El ID 1022345 ya ganó el Premio 'Televisor 55'".
* **Por qué:** Esto demuestra al auditor que la regla de "un premio por persona" se ejecutó sin errores.

---

## ⚡ Acciones de Exportación (El toque de C#)

Para tu portafolio, aquí es donde integras librerías potentes en el backend:

1. **Exportar Acta PDF:** Usa una librería como `QuestPDF` o `iTextSharp` en .NET. El PDF debe tener el logo del banco, una estructura formal y un espacio para firmas.
2. **Exportar a Excel (XLSX):** Usa `ClosedXML` o `EPPlus`. Es vital para que el banco cruce los datos con su sistema de contabilidad.
3. **Link al Archivo Original (S3):** Un botón para que el auditor pueda descargar el archivo plano original que se subió (el que está bajo **Object Lock**), demostrando que la data no fue alterada.

--- 

**Mi sugerencia:** Implementar un **Idempotency Check** y un estado de `IsArchived` en la base de datos. Si el estado es `Finalizado`, todos los endpoints de `POST/PUT` deben devolver `403 Forbidden`.

---

## 🛠️ Tareas Finales de esta Fase:

1. **Angular:** Crear `ReporteComponent`, implementar filtros de búsqueda en la tabla de ganadores.
2. **C#:** Crear los servicios de generación de documentos (PDF/Excel).
3. **SQL:** Asegurar que la tabla `Ganadores` tenga todos los campos de auditoría necesarios (Foreign Keys a Premios y Clientes).

---

## 🏁 Conclusión del Diseño de Pantallas

¡Felicidades! Hemos diseñado un producto completo:

1. **Login/Auth:** Acceso seguro y multitenant.
2. **Dashboard:** Control de estados.
3. **Setup:** Gestión dinámica de premios.
4. **Ingesta:** Procesamiento masivo y seguridad de datos.
5. **Lobby:** Punto de control y reset.
6. **Arena Live:** El "show" en tiempo real con SignalR.
7. **Reportes:** Auditoría y cierre legal.

# ⚡ FlashFortune

**FlashFortune** is a high-performance, enterprise-grade sweepstakes management engine designed for the financial sector. It enables banks to process up to **100 million virtual coupons** in real-time with absolute auditability, cryptographic fairness, and a "security-first" approach to sensitive financial data.

---

## 🚀 The Challenge

Financial institutions often struggle with legacy sweepstakes systems that are opaque, slow, and pose security risks by storing sensitive balance data. **FlashFortune** solves this by using a **Format-Preserving Encryption (FPE)** approach to virtualize coupons and an automated data-sanitization pipeline.

## 🛠️ Tech Stack

* **Backend:** .NET 8 (C#) following **Clean Architecture** principles.
* **Frontend:** Angular 17/18 with **Signals** and **Tailwind CSS**.
* **Real-Time:** **SignalR** for synchronized, low-latency drawing events.
* **Database:** **PostgreSQL** for persistent storage.
* **Performance Cache:** **Redis** for instant winner exclusion checks.
* **Background Processing:** **Hangfire** for asynchronous, non-blocking file ingestion.
* **Storage:** **AWS S3** with **Object Lock** for immutable audit trails.

---

## ✨ Key Features

### 1. High-Velocity Virtual Engine

Instead of bloating the database with millions of rows, FlashFortune uses a **Deterministic Permutation Algorithm**. It maps 100M+ coupons virtually, ensuring non-continuous distribution and O(1) look-up times.

### 2. "Banking-Grade" Security

* **Data Hygiene:** Sensitive account balances are used only for coupon calculation and are **permanently purged** from the operational DB post-ingestion.
* **Audit Vault:** The original source-of-truth files are stored in S3 with **WORM (Write Once, Read Many)** policies.
* **Multi-Tenancy:** Strict logical isolation between different financial entities.

### 3. Real-Time "Arena" Experience

A fully synchronized live-drawing interface. Using **SignalR**, the "Arena" provides a high-stakes visual experience for TV or live-streamed events, featuring:

* Real-time "RNG" scrolling animations.
* Automatic collision handling (Winner exclusion logic).
* **The Panic Button:** Instant reset capabilities for individual prizes without compromising the entire draw integrity.

### 4. Comprehensive Compliance

Generates cryptographically signed PDF and Excel reports, including a full **Exclusion Log** for auditors to verify the fairness of the "one prize per person" rule.

---

## 🏗️ Architecture Overview

The project follows **Clean Architecture** to ensure testability and maintainability:

* **Domain:** Pure entities and business rules.
* **Application:** MediatR commands/queries and the core Shuffle logic.
* **Infrastructure:** EF Core, Redis implementation, S3 Storage, and Hangfire jobs.
* **API:** Slim controllers and SignalR Hubs.

---

## 📦 Getting Started

### Prerequisites

* Docker & Docker Compose
* .NET 8 SDK
* Node.js & Angular CLI

### Installation

1. **Clone the repo:**

```
git clone https://github.com/cmogrovejo/FlashFortune

```
2. **Spin up infrastructure (DB, Redis, S3 LocalStack):**

```
docker-compose up -d

```
3. **Run the Backend:**

```
cd src/FlashFortune.API
dotnet run

```
4. **Run the Frontend:**

```
cd src/FlashFortune-UI
npm install && ng serve

```

---

## 📜 License

This project is licensed under the MIT License.

---

**Author:** Carlos Antonio Mogrovejo

**LinkedIn:** https://www.linkedin.com/in/cmogrovejo/

*Developed as a showcase of scalable architecture and financial software integrity.*

---

### PM's Final Note on the README:

This README positions you as a **Solutions Architect**. It explicitly mentions **SignalR**, **Clean Architecture**, and **Deterministic Permutation**, which are keywords that trigger "Senior" flags in recruiters' minds.

---

