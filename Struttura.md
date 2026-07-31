# Enterprise Vue.NET Framework 2026

## Visione, Architettura e Linee Guida

> Versione 1.0

---

# Introduzione

Questo repository rappresenta il framework base utilizzato per tutti i futuri progetti dell'azienda.

L'obiettivo principale è evitare di reinventare continuamente l'architettura, mantenere uno standard qualitativo elevato e garantire che ogni nuovo progetto parta già con una struttura enterprise, moderna e scalabile.

Ogni componente del framework deve essere:

- Riutilizzabile
- Disaccoppiato
- Testabile
- Documentato
- Configurabile
- Modularizzabile

Il framework deve poter essere utilizzato per:

- SaaS
- CRM
- ERP
- Dashboard
- Portali B2B
- Portali B2C
- Landing Page
- Gestionali
- Marketplace
- Applicazioni Mobile (tramite API)
- Desktop App (Electron)

---

# Filosofia

L'architettura segue alcuni principi fondamentali.

## 1. Feature First

Il codice viene organizzato per funzionalità e non per tipologia.

NO

```
components/
services/
stores/
models/
```

SI

```
features/
    users/
    auth/
    dashboard/
    settings/
```

Ogni feature contiene tutto ciò che le appartiene.

---

## 2. Modularità

Ogni modulo deve poter essere:

- installato
- rimosso
- aggiornato

senza impattare il resto del progetto.

---

## 3. Riutilizzo

Ogni componente sviluppato deve poter essere riutilizzato.

Se una funzionalità potrebbe servire in almeno due progetti differenti, deve essere spostata nel Framework Core.

---

## 4. Separazione delle responsabilità

Ogni livello dell'applicazione deve avere una singola responsabilità.

UI

↓

Business Logic

↓

Application Logic

↓

Infrastructure

↓

Persistence

---

## 5. Dependency Rule

I livelli interni non devono conoscere quelli esterni.

Il Domain non deve conoscere Entity Framework.

L'Application non deve conoscere PostgreSQL.

La UI non deve conoscere Axios.

---

# Stack Tecnologico

## Frontend

- Vue 3
- TypeScript
- Vite
- Tailwind CSS v4
- Pinia
- TanStack Query
- Vue Router
- Motion
- VueUse
- Zod
- Axios
- Heroicons
- ESLint
- Prettier
- Vitest
- Playwright

---

## Backend

- .NET 10
- ASP.NET Core Minimal API
- Entity Framework Core
- PostgreSQL
- FluentValidation
- MediatR
- Serilog
- JWT
- Refresh Token
- Redis
- OpenAPI

---

## DevOps

- Docker
- Docker Compose
- GitHub Actions
- Husky
- Commitlint
- Conventional Commits
- Dependabot
- CodeQL

---

# Struttura del Repository

```
enterprise-framework/

apps/
    api/
    web/

packages/
    ui/
    sdk/
    shared/
    types/

modules/

docs/

docker/

scripts/

.github/

tools/
```

---

# Il concetto di Modulo

Il Framework è composto da moduli indipendenti.

Ogni modulo rappresenta una funzionalità completa.

Esempi:

```
Auth

Users

Roles

Permissions

Notifications

Email

Storage

Localization

Dashboard

Calendar

Chat

Payments

Audit

Logs

Files

Settings

CMS

AI

Reporting
```

Ogni modulo deve poter essere:

✔ installato

✔ disinstallato

✔ aggiornato

✔ configurato

senza modificare altri moduli.

---

# Struttura di un modulo

```
modules/

users/

README.md

module.json

frontend/

backend/

shared/

tests/
```

---

## module.json

Ogni modulo possiede un file descrittivo.

```
{
    "name": "Users",
    "version": "1.0.0",
    "dependencies": [
        "Auth"
    ],
    "enabled": true
}
```

---

# Architettura Frontend

```
src/

app/

core/

shared/

features/

layouts/

router/

plugins/

assets/

types/
```

---

# app

Bootstrap.

Configurazioni.

Provider.

Dependency Injection.

---

# core

Contiene tutto ciò che esiste una sola volta.

```
api

auth

http

config

storage

errors

logger

permissions

interceptors
```

---

# shared

Codice condiviso.

```
components

composables

utils

hooks

types

constants

validators
```

---

# features

Ogni cartella rappresenta una funzionalità.

```
users/

components

pages

api

stores

composables

types

validators

routes
```

---

# Design System

Il Design System rappresenta una libreria indipendente.

```
packages/

ui/

Button/

Input/

Dialog/

Modal/

Table/

Avatar/

Tooltip/

Badge/

Card/
```

Ogni componente contiene:

```
Button.vue

Button.types.ts

Button.test.ts

Button.stories.ts

index.ts
```

---

# Design Tokens

Mai utilizzare colori hardcoded.

Utilizzare sempre token.

```
Primary

Secondary

Success

Warning

Danger

Background

Surface

Border

Radius

Spacing

Shadow

Typography
```

---

# Theme Engine

Il framework deve supportare:

- Light
- Dark
- Custom Theme

senza modificare i componenti.

---

# Gestione Stato

Separazione tra:

Client State

↓

Pinia

Server State

↓

TanStack Query

Mai utilizzare Pinia per dati recuperati dalle API.

---

# Gestione API

Ogni chiamata passa attraverso:

```
Feature Service

↓

Api Client

↓

Http Client

↓

Axios
```

Mai utilizzare Axios direttamente.

---

# SDK

L'SDK viene generato automaticamente da OpenAPI.

```
API

↓

OpenAPI

↓

SDK Generator

↓

packages/sdk
```

Il frontend utilizza esclusivamente l'SDK.

---

# Gestione Errori

Gerarchia.

```
ApplicationError

ValidationError

BusinessError

UnauthorizedError

ForbiddenError

NetworkError
```

---

# Logging

Frontend

```
Logger

↓

Console

↓

Sentry
```

Backend

```
Serilog

↓

Seq

↓

Elastic
```

---

# Sicurezza

Ogni progetto deve includere:

JWT

Refresh Token

CSRF Protection

Rate Limiting

Security Headers

CORS

Audit Log

Encryption

Secret Management

Role Based Access Control

Permission Based Access Control

---

# Database

Architettura

```
Domain

↓

Application

↓

Infrastructure

↓

Persistence
```

Le Entity non devono essere esposte al frontend.

---

# Testing

Ogni modulo deve contenere:

Unit Test

Integration Test

End-to-End Test

Snapshot Test

Accessibility Test

---

# Documentazione

Ogni modulo deve avere un README.

La cartella docs conterrà:

```
architecture.md

frontend.md

backend.md

coding-guidelines.md

api.md

security.md

deployment.md

design-system.md

conventions.md

branching.md

modules.md
```

---

# Convenzioni

## Naming

Componenti

PascalCase

Funzioni

camelCase

Cartelle

kebab-case

Costanti

SCREAMING_SNAKE_CASE

Interface

IUser

Enum

UserRole

---

# Git Workflow

Main

Produzione

Develop

Sviluppo

Feature

Nuove funzionalità

Fix

Correzioni

Release

Versioni

Hotfix

Correzioni urgenti

---

# Continuous Integration

Ad ogni Pull Request:

1. Installazione dipendenze
2. Lint
3. Type Check
4. Unit Test
5. Build
6. Docker Build
7. Security Scan
8. Code Coverage

---

# Obiettivi del Framework

- Architettura coerente in tutti i progetti.
- Elevata riusabilità del codice.
- Riduzione del tempo di avvio di nuovi progetti.
- Aggiornamenti centralizzati.
- Esperienza di sviluppo uniforme.
- Facilità di manutenzione.
- Elevata testabilità.
- Scalabilità orizzontale.
- Possibilità di distribuire i moduli come pacchetti indipendenti.

## Modulo Realtime (SignalR)

Il framework integra nativamente **ASP.NET Core SignalR** come infrastruttura per tutte le comunicazioni realtime.

Il modulo deve essere completamente indipendente e attivabile tramite configurazione.

### Obiettivi

- Notifiche realtime
- Chat
- Aggiornamento dashboard
- Aggiornamento tabelle senza refresh
- Progress di processi lunghi
- Eventi di sistema
- Monitoraggio utenti connessi
- Presenza utenti (Online/Offline)
- Broadcast di eventi
- Comunicazione server → client
- Comunicazione client → server
- Supporto WebSocket con fallback automatico

### Struttura

```text
modules/

realtime/

README.md
module.json

frontend/
    composables/
    services/
    stores/
    types/

backend/
    Hubs/
    Services/
    Events/
    Contracts/
    Extensions/

shared/

tests/
```

---

## Backend

### Hubs

Ogni dominio può esporre uno o più Hub.

```text
Hubs/

NotificationHub

ChatHub

DashboardHub

PresenceHub

WorkflowHub
```

Gli Hub devono contenere esclusivamente la gestione delle connessioni e l'instradamento dei messaggi.

La logica di business deve risiedere nei Service.

---

### Event Bus

Il framework adotta un'architettura Event-Driven.

Ogni modulo può pubblicare eventi senza conoscere i destinatari.

Esempio:

```text
UserCreated

↓

Event Bus

↓

Notification Module

↓

SignalR Hub

↓

Client Vue
```

Questa architettura mantiene i moduli disaccoppiati e facilita l'estensione del sistema.

---

### Gestione Connessioni

Il framework mantiene un registro centralizzato delle connessioni.

Informazioni gestite:

- ConnectionId
- UserId
- TenantId (per Multi-Tenant)
- Ruoli
- Gruppi
- Ultima attività
- Stato Online/Offline

---

### Gruppi

SignalR deve utilizzare i Group per separare gli utenti.

Esempi:

- Azienda
- Tenant
- Team
- Reparto
- Progetto
- Chat
- Dashboard
- Ruolo

---

### Eventi Standard

Il framework definisce una serie di eventi comuni.

```text
Connected
Disconnected

NotificationReceived

UserOnline
UserOffline

MessageReceived

ProgressUpdated

EntityCreated
EntityUpdated
EntityDeleted

PermissionChanged

ThemeChanged

ConfigurationUpdated
```

Ogni modulo può pubblicare eventi aggiuntivi.

---

## Frontend

Il frontend non utilizza direttamente SignalR.

Tutte le comunicazioni passano attraverso un servizio centralizzato.

```text
Vue Component

↓

Composable

↓

Realtime Service

↓

SignalR Client
```

In questo modo è possibile sostituire il provider realtime senza modificare i componenti.

---

### Composables

```text
useRealtime()

useNotifications()

usePresence()

useChat()

useProgress()

useDashboard()
```

---

### Auto Reconnect

Il framework deve gestire automaticamente:

- riconnessione
- heartbeat
- retry esponenziale
- gestione perdita connessione
- ripristino gruppi
- refresh token durante la riconnessione

---

### Gestione Offline

Quando la connessione cade:

- memorizzazione degli eventi pendenti
- sincronizzazione alla riconnessione
- indicatori di stato
- modalità degradata

---

## Integrazione con TanStack Query

Quando un evento SignalR modifica dati presenti nella cache:

```text
SignalR Event

↓

Realtime Service

↓

Query Invalidation

↓

Aggiornamento automatico UI
```

L'obiettivo è evitare refresh manuali delle pagine e mantenere la cache sempre coerente.

---

## Integrazione con Notifications

Il modulo Notification utilizza SignalR come canale realtime.

Supporta:

- Toast
- Badge
- Centro notifiche
- Push interne
- Avvisi di sistema

---

## Sicurezza

Tutte le connessioni devono essere protette tramite JWT.

Ogni Hub deve verificare:

- autenticazione
- autorizzazione
- tenant corrente
- permessi richiesti

L'accesso ai gruppi deve essere gestito esclusivamente dal server.

---

## Scalabilità

L'implementazione deve essere predisposta per ambienti distribuiti.

Supportare:

- Redis Backplane
- Azure SignalR Service
- Bilanciamento del carico
- Più istanze API
- Sticky Session quando necessarie

---

## Obiettivo Architetturale

SignalR rappresenta il livello di comunicazione realtime del framework e deve essere completamente trasparente alle feature. I moduli pubblicano eventi di dominio, mentre il modulo Realtime decide come distribuirli ai client. Questo garantisce disaccoppiamento, elevata testabilità e la possibilità di sostituire l'infrastruttura di trasporto senza modificare la logica applicativa.

---

# Roadmap

## Foundation

- Setup repository
- CI/CD
- Docker
- Logging
- Configurazione ambiente
- Design System
- Theme Engine

## Core Modules

- Authentication
- Users
- Roles
- Permissions
- Notifications
- Settings
- File Storage
- Audit
- Localization

## Business Modules

- Dashboard
- Calendar
- Chat
- CMS
- Reporting
- AI Assistant
- Payments
- Workflow Engine

## Enterprise Features

- Multi-Tenant
- Plugin Marketplace
- Feature Flags
- Event Bus
- CQRS
- Background Jobs
- Distributed Cache
- Realtime (SignalR)
- Offline Support
- Observability
- Telemetria
- Performance Monitoring

---

# Visione Finale

Questo framework non rappresenta un singolo progetto, ma una piattaforma applicativa enterprise pensata per evolversi nel tempo. Ogni nuovo progetto dovrà essere costruito componendo moduli indipendenti e riutilizzabili, mantenendo un'architettura coerente, una qualità del codice elevata e un'esperienza di sviluppo uniforme.

L'obiettivo è arrivare a un ecosistema in cui creare un nuovo prodotto richieda principalmente la selezione dei moduli necessari, la configurazione delle impostazioni specifiche e lo sviluppo della sola logica di business, riducendo drasticamente il tempo di avvio e aumentando affidabilità, manutenibilità e scalabilità.
