# MessageBrokerApp - Trifork Kodetest

## Introduktion

Dette projekt er en løsning til en kodetest for Trifork. Opgaven består i at udvikle et system med to .NET 8-applikationer (`MessageProducer` og `MessageConsumer`), der kommunikerer via en message broker (RabbitMQ) og gemmer udvalgte data i en PostgreSQL-database. Hele infrastrukturen (RabbitMQ, PostgreSQL, pgAdmin) køres lokalt via Docker Compose.

**Kernefunktionalitet:**

* **MessageProducer:** Sender kontinuerligt beskeder med et `Timestamp` og en `Counter` til en RabbitMQ-kø.
* **MessageConsumer:** Lytter på køen og behandler beskeder:
    * Kasserer beskeder ældre end 1 minut.
    * Gemmer beskeder i PostgreSQL, hvis sekundet i `Timestamp` er lige (og < 1 min gammel).
    * Sender beskeder tilbage i køen med forøget `Counter`, hvis sekundet er ulige (op til `MaxRetries`, pt. 5).

**Anvendte Teknologier:**

* .NET 8 (Console Applications med Generic Host, DI, Logging, Configuration)
* RabbitMQ (via `RabbitMQ.Client` NuGet-pakke)
* PostgreSQL (via `Npgsql` NuGet-pakke)
* Docker & Docker Compose
* NUnit (til Unit Tests)

## Forudsætninger

For at kunne bygge og køre projektet skal følgende være installeret:

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Version 8.0.405 eller nyere - se `global.json`)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) (eller Docker Engine/CLI + Docker Compose på Linux)

## Opsætning af Infrastruktur

Infrastrukturen (RabbitMQ, PostgreSQL, pgAdmin) startes nemt via Docker Compose:

1.  Åbn en terminal eller kommandoprompt.
2.  Navigér til roden af dette projekt (mappen hvor `docker-compose.yml` ligger).
3.  Kør kommandoen:
    ```bash
    docker-compose up -d
    ```
    * `-d` flaget starter containerne i "detached mode" (i baggrunden).
    * Første gang kan det tage lidt tid at downloade Docker images.

4.  Vent et øjeblik på, at containerne starter helt op. Du kan tjekke status med `docker ps`.

## Kørsel af Applikationerne

Applikationerne (`MessageProducer` og `MessageConsumer`) skal køres i separate terminalvinduer.

1.  Åbn **to** separate terminaler/kommandoprompter.
2.  Navigér til roden af projektet i **begge** terminaler.
    - Eller bevæg dig ind i hver sub-mappe, og kør almindelig ```dotnet run```
3.  **Terminal 1 (Producer):** Kør følgende kommando for at starte `MessageProducer`:
    ```bash
    dotnet run --project MessageProducer
    ```
4.  **Terminal 2 (Consumer):** Kør følgende kommando for at starte `MessageConsumer`:
    ```bash
    dotnet run --project MessageConsumer
    ```

* Produceren vil begynde at sende beskeder (output kan være begrænset afhængig af log-niveau).
* Consumeren vil starte, forbinde og begynde at behandle beskeder fra køen (output viser behandlingslogik).

**Stop Applikationerne:** Tryk `Ctrl+C` i hver terminal for at stoppe Producer og Consumer pænt.

**Stop Infrastruktur:** Kør `docker-compose down` i terminalen fra projektets rodmappe for at stoppe og fjerne Docker-containerne. Data i PostgreSQL-databasen vil blive bevaret i et Docker volume (`pgdata`), medmindre du manuelt fjerner voluminet.

## Projektstruktur

* **`MessageProducer/`**: .NET 8 Konsolapplikation, der sender beskeder til RabbitMQ.
* **`MessageConsumer/`**: .NET 8 Konsolapplikation, der modtager og behandler beskeder fra RabbitMQ og interagerer med PostgreSQL.
* **`MessageShared/`**: .NET 8 Klassebibliotek, der indeholder den fælles `Message`-klasse, som bruges af både Producer og Consumer.
* **`MessageConsumer.Tests/`**: NUnit-testprojekt til unit tests af logikken i `MessageConsumer` (primært `MessageHandler`).
* **`docker-compose.yml`**: Definerer og konfigurerer Docker-containere for RabbitMQ, PostgreSQL og pgAdmin.
* **`appsettings.json` / `appsettings.Development.json`**: Konfigurationsfiler for applikationerne (RabbitMQ hostname, PostgreSQL connection string, log-niveauer).

## Adgang til Services (Valgfrit)

Du kan tilgå management interfaces for RabbitMQ og pgAdmin via din browser:

* **RabbitMQ Management UI:**
    * URL: [http://localhost:15672](http://localhost:15672)
    * Bruger: `guest`
    * Password: `guest`
    * Her kan du se køen `Message-Queue`, forbindelser, osv.

* **pgAdmin 4 (Database Admin Tool):**
    * URL: [http://localhost:8080](http://localhost:8080)
    * Bruger: `admin@admin.com`
    * Password: `admin`
    * **NB:** Første gang du logger ind, skal du manuelt tilføje en serverforbindelse til PostgreSQL-databasen:
        * Klik "Add New Server".
        * Giv den et navn (f.eks. "Local Docker Postgres").
        * Gå til "Connection"-tabben:
            * Host name/address: `localhost`
            * Port: `5433` (Bemærk: Ikke standard 5432 pga. mapping i `docker-compose.yml`)
            * Maintenance database: `messagesdb`
            * Username: `appuser`
            * Password: `secret`
        * Klik "Save".
    * Du kan nu browse `messagesdb`-databasen og se `messages`-tabellen.

## Kørsel af Tests

Unit tests for `MessageConsumer` kan køres fra terminalen:

1.  Navigér til roden af projektet.
2.  Kør kommandoen:
    ```bash
    dotnet test
    ```