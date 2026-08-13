CQRS stands for Command Query Responsibility Segregation.

At its core, it is an architectural design pattern that strictly separates the operations that mutate data from the operations that read data.

Commands (Write Side): Actions that change the state of a system (Create, Update, Delete). They execute complex business logic and validation, but they do not return application data.

Queries (Read Side): Actions that retrieve data. They return information directly to the user interface but never modify the database or trigger side effects.

By separating these concerns, you can scale and optimize your read and write operations independently. For instance, you might use a highly normalized relational database to safely process commands, and a flat, denormalized NoSQL cache to serve queries at lightning speed.

Step-by-Step Pseudo-code: Bank Account Scenario
Here is how you would build a CQRS flow for a banking system where users can deposit money and check their balance.

1. Separate the Data Models
In a traditional system, you use one Account model for everything. In CQRS, you create separate models tailored to their specific job.

Plaintext
// Write Model: Optimized for strict rules and transaction safety
class BankAccount {
    String accountId;
    Decimal balance;
    String status;
}

// Read Model: A flattened view optimized for the UI
class AccountSummaryView {
    String accountId;
    String ownerName;
    Decimal currentBalance;
}
2. Define the Commands and Queries
Commands represent the intent to change state. Queries represent a request for data.

Plaintext
// COMMAND: Intent to mutate state
class DepositMoneyCommand {
    String accountId;
    Decimal amount;
}

// QUERY: Intent to read state
class GetAccountSummaryQuery {
    String accountId;
}
3. Build the Command Handler (Write Logic)
This handler receives commands, enforces your business rules, and saves the updated state to the write database.

Plaintext
class AccountCommandHandler {
    WriteDatabase writeDb;

    function handle(DepositMoneyCommand cmd) {
        // 1. Validate
        if (cmd.amount <= 0) {
            throw Error("Deposit amount must be positive");
        }
        
        // 2. Execute Business Logic
        BankAccount account = writeDb.findById(cmd.accountId);
        account.balance += cmd.amount;
        
        // 3. Save State
        writeDb.save(account);
        
        // 4. (Optional) Publish an event so the Read database knows to update itself
        EventBus.publish(new MoneyDepositedEvent(cmd.accountId, cmd.amount));
    }
}
4. Build the Query Handler (Read Logic)
This handler's only job is to fetch data as fast as possible. Because the read model is already structured for the UI, there is no need for complex joins or business logic here.

Plaintext
class AccountQueryHandler {
    ReadDatabase readDb; // Often a fast NoSQL store or Redis cache

    function handle(GetAccountSummaryQuery query) returns AccountSummaryView {
        // Direct, fast read. No locks, no validation.
        return readDb.getSummaryView(query.accountId);
    }
}
5. Wire it up in the API Controller
The controller no longer contains business logic. It simply routes incoming HTTP requests to either the Command system or the Query system.

Plaintext
class BankAccountController {
    CommandBus commandBus;
    QueryBus queryBus;

    // POST: Mutates data. Returns a simple success status, not the data itself.
    function depositMoney(Request req) {
        Command cmd = new DepositMoneyCommand(req.id, req.amount);
        commandBus.send(cmd);
        
        return Status(202, "Deposit Accepted"); 
    }

    // GET: Reads data. Has no side effects.
    function getSummary(String id) {
        Query query = new GetAccountSummaryQuery(id);
        AccountSummaryView view = queryBus.fetch(query);
        
        return Response(view);
    }
}
Want to learn how Event Sourcing pairs with CQRS?

When you pair Command Query Responsibility Segregation (CQRS) with Event Sourcing, you fundamentally change how you store data. Instead of saving the current state of an object (like saving a balance of $100), you save a chronological log of every event that led to that state (e.g., "Account Opened," "Deposited $50," "Deposited $50").

Event Sourcing acts as the perfect bridge between the Write side and the Read side of a CQRS architecture.

Here is how they work together to keep everything synchronized:

The Synchronization Flow
When a user performs an action, the flow moves from the Write side, through an Event Bus, to the Read side.

1
Command is Handled (Write Side)
Validating the action
The application receives a command (e.g., DepositMoneyCommand). The Command Handler fetches the current state of the entity by replaying its past events from the Event Store, validates the new command against business rules, and if successful, generates a new event (e.g., MoneyDepositedEvent).

2
Event is Appended
The source of truth
The new event is appended to the Event Store. This database is append-only; events are never updated or deleted. This serves as your authoritative write database.

3
Event is Published
Broadcasting the change
Immediately after saving to the Event Store, the system publishes the event to an Event Bus or message broker (like Kafka, RabbitMQ, or AWS SNS/SQS).

4
Read Model is Updated (Read Side)
Projecting the data
The Query side listens to the Event Bus. When it receives the MoneyDepositedEvent, an Event Handler (often called a "Projector") updates the Read Database (e.g., a fast NoSQL cache). It takes the event data and updates the flat, denormalized tables so they are instantly ready for the UI to query.

Why They Are a Perfect Match
CQRS and Event Sourcing are often used together because they solve each other's biggest problems.

The CQRS Problem: How do you keep the separate Write and Read databases in sync without complex distributed transactions?

The Event Sourcing Solution: By using events as the source of truth. The Write side generates the events, and the Read side just listens to them and updates itself (a pattern known as Eventual Consistency).

The Event Sourcing Problem: Querying an append-only log of events is terrible for UI performance. You can't easily ask, "Give me a list of all accounts with a balance over $1,000," without replaying every event in the system.

The CQRS Solution: The Read side creates optimized "projections." It constantly listens to events and updates a separate database that is perfectly shaped for those exact UI queries.

Rebuilding the Read Database
One of the biggest superpowers of this combination is replayability.

Because your Event Store contains the entire history of everything that ever happened in the system, your Read database is technically completely disposable. If you decide your UI needs a new dashboard, or if your Read database gets corrupted, you can simply delete the Read database, spin up a new one, and replay every event from the beginning of time to rebuild the current state perfectly.

To explore the implications of this architecture:

What is Eventual Consistency?

How to handle event schema changes

n Event Sourcing, the golden rule is that the Event Store is an append-only, immutable log. You cannot go back and rewrite history to make old events match a new schema.

If you change a UserRegistered event to require a DateOfBirth, your system still has to know how to read the thousands of old UserRegistered events that don't have that field.

Here are the four standard strategies for handling event evolution, ranked from most common to least common.

1. Upcasting (The Industry Standard)
Upcasting handles migrations on the fly. You leave the old events exactly as they are in the database. When the application reads the event stream, a middleware component called an Upcaster intercepts old V1 events and transforms them into V2 events in memory before the application logic sees them.

Your core domain logic only ever has to know about the newest version (V2).

Plaintext
// The Upcaster intercepts the raw JSON/data before deserialization
class UserRegisteredUpcaster {
    
    function upcast(Event event) {
        if (event.version == 1) {
            // Transform V1 to V2 by providing a default value for the new field
            return new Event(
                version = 2,
                userId = event.data.userId,
                name = event.data.name,
                dateOfBirth = "UNKNOWN" // The new field
            );
        }
        return event;
    }
}
2. Additive Changes (Weak Schema)
If a change is purely additive and doesn't break existing logic, you don't necessarily need a new version. You simply add the new field to your event class and make it optional or nullable.

When your system reads an old event from the database, the deserializer simply assigns null to the new field. Your application logic must be written to handle that null gracefully. This is the easiest approach but requires strict discipline to ensure you never make breaking changes (like renaming or deleting fields).

3. Multiple Event Handlers (Versioned Code)
Instead of transforming the event before it reaches the domain, you allow both versions of the event to exist in your codebase. Your Aggregate (the write model) and your Projectors (the read models) simply implement handlers for both versions.

Plaintext
class UserAggregate {
    String name;
    String dateOfBirth;

    // Handler for the old version
    function apply(UserRegistered_V1 event) {
        this.name = event.name;
        this.dateOfBirth = "UNKNOWN";
    }

    // Handler for the new version
    function apply(UserRegistered_V2 event) {
        this.name = event.name;
        this.dateOfBirth = event.dateOfBirth;
    }
}
The downside: Over years of development, your classes can become heavily cluttered with legacy handlers (V1, V2, V3, V4...).

4. Stream Migration (The Last Resort)
If the changes are so massive that upcasting becomes a severe performance bottleneck, you perform a Stream Migration.

You write a one-off script that reads the entire old event stream, translates every single event into the new schema, and writes them to a brand new stream (e.g., migrating Customer-123 to Customer-123-V2). Once the migration is verified, you point your application at the new stream and archive the old one.

This is rare because it is complex, requires system downtime or tricky concurrent write logic, and technically violates the pure philosophy of never altering the historical log.

Key Takeaway: Always prefer Upcasting for breaking changes. It keeps your domain logic clean (only dealing with the latest version) while preserving the absolute truth of your historical data.

How do you handle GDPR 'Right to be Forgotten'?

Yes
In an append-only Event Sourcing system, how do you handle GDPR and the 'Right to be Forgotten' if events are immutable and cannot be deleted?

The collision between Event Sourcing (which dictates that historical data must never be altered or deleted) and regulations like GDPR (which legally mandate that Personally Identifiable Information, or PII, must be erasable) is one of the most famous architectural challenges in modern software design.

If your event stream contains UserRegistered(Name="Alice", Email="alice@example.com"), you cannot simply append a UserDeleted event. While that updates your read models, the original PII still exists in the immutable event log, which violates GDPR.

To solve this, the industry relies on three main strategies.

1. Crypto-Shredding (The Gold Standard)
Crypto-shredding (or Cryptographic Erasure) is the most elegant and widely adopted solution. Instead of trying to delete the data from the immutable log, you make the data mathematically impossible to read.

1
Generate a unique key
When a new user is created, generate a unique cryptographic encryption key specifically for that user. Store this key in a secure, mutable database (a Key Management System or a simple SQL table).

2
Encrypt the PII
Only encrypt the PII, not the whole event
Before appending an event to the Event Store, encrypt any PII fields (name, email, address) using that user's unique key. The non-PII data (account status, timestamp, transaction amounts) remains in plain text.

3
Process normally
When your read models or domain logic need to read the event stream, they fetch the event, fetch the user's key, decrypt the PII on the fly, and process the data.

4
Shred the key
The Right to be Forgotten
When a user requests deletion, you simply delete their encryption key from the mutable key database. The events in the Event Store remain untouched, but the PII inside them is instantly transformed into useless cryptographic noise. Legally and practically, the data is gone.

2. PII Segregation (The Pragmatic Approach)
If you do not want the overhead of managing thousands of encryption keys, you can simply refuse to put PII in the Event Store in the first place.

The Architecture: You maintain your immutable Event Store, but you also maintain a traditional, mutable CRUD database specifically for user profiles.

The Flow: When a user registers, their Name and Email go into the Users SQL table. The Event Store only receives a UserRegistered(UserId="12345") event, containing a meaningless UUID.

The Deletion: When a GDPR request comes in, you execute a standard DELETE FROM Users WHERE UserId="12345". Your event stream remains intact for statistical and historical purposes, but the anonymous UserId can no longer be linked to a real human.

3. Stream Rewriting (The Nuclear Option)
If a system was built without Crypto-Shredding or PII Segregation and a GDPR request comes in, you have no choice but to break the rules of Event Sourcing and rewrite history.

This involves reading the user's entire event stream, scrubbing the PII out of the payloads in memory, and writing the scrubbed events to a brand new stream. Once the new stream is verified, the old stream is hard-deleted from the database.

This is incredibly risky, complex, and time-consuming. It violates the core philosophy of immutability and should only be used as a last resort to retrofit legacy event stores.

Key insight: A GDPR deletion request rarely means "delete everything this user ever did." If a user bought a product, financial compliance laws require you to keep the transaction record for taxes. By using Crypto-Shredding or Segregation, you successfully erase the person while keeping the vital business metrics intact in your event stream.

To go deeper into Event Sourcing architecture:

How to handle concurrency

When NOT to use Event Sourcing
How do you handle concurrency and prevent race conditions when multiple commands hit the same event stream simultaneously?

In Event Sourcing, race conditions happen when two concurrent commands try to modify the exact same entity (Aggregate) at the exact same time.

Imagine two users sharing a bank account with a $100 balance. They both hit "Withdraw $100" simultaneously. Both command handlers read the event stream, see a $100 balance, validate the rule (balance ≥ 100), and attempt to append a MoneyWithdrawn($100) event. If both succeed, the account balance drops to -$100, breaking your business rules.

To prevent this, the industry standard is Optimistic Concurrency Control (OCC), backed by database-level constraints.

Here is how it works.

Optimistic Concurrency Control
Instead of locking the database (which kills performance), OCC assumes conflicts are rare. It lets both commands execute their logic, but uses event versions to ensure only one of them can actually save their result.

Every event in a stream is assigned a sequential version number. The Event Store database is configured to enforce a strict unique constraint on [StreamId, Version].

1
Read the Stream
Noting the version number
Command A and Command B both fetch the event stream for Account 123. The stream currently has 5 events. The application loads the state and notes that the Expected Version is 5.

2
Process the Command
Executing the domain logic
Both handlers validate their withdrawals against the current balance. Both succeed and generate a new MoneyWithdrawn event. Because the expected version was 5, both assign their new event Version 6.

3
Attempt to Append
The atomic database transaction
Command A reaches the database a millisecond faster. It says, "Append this event as Version 6 for Account 123." The database checks, sees no Version 6 exists, and writes it successfully.

4
The Collision
The concurrency exception
Command B arrives a millisecond later and says, "Append this event as Version 6 for Account 123." The database rejects it because Version 6 already exists. It throws a ConcurrencyException.

How the Database Enforces It
If you are using a relational database like PostgreSQL for your Event Store, this is handled trivially with a composite unique index:

SQL
ALTER TABLE events ADD CONSTRAINT unique_stream_version UNIQUE (stream_id, version);
If you are using NoSQL databases like DynamoDB, you use "Conditional Writes" (e.g., attribute_not_exists(version)). Dedicated event stores like EventStoreDB handle this natively.

Handling the Concurrency Exception
When Command B fails and throws a ConcurrencyException, your application must decide how to react. You have two main options:

Fail Fast: Catch the exception, return an HTTP 409 Conflict to the UI, and tell the user, "This record was modified by someone else. Please refresh and try again." This is safe and easy to implement.

Automatic Retry: Catch the exception and automatically re-run the command from scratch. The system will re-read the stream (which is now at Version 6), re-validate the business rules (the balance is now $0), and correctly reject the withdrawal with an "Insufficient Funds" error. The user never knows a concurrency collision happened.

The Alternative: The Actor Model (Single-Threaded)
If your system expects a massive amount of simultaneous writes to the same aggregate (e.g., thousands of bids on a single auction item per second), OCC will result in too many exceptions and retries, crushing performance.

In these high-contention scenarios, developers often pair Event Sourcing with the Actor Model (using frameworks like Akka or Microsoft Orleans).

In the Actor Model, an Aggregate is loaded into memory as a dedicated "Actor." Every command destined for that Aggregate is placed in a queue and processed strictly one at a time, synchronously. Because the Actor processes commands sequentially on a single thread, race conditions are structurally impossible, and you never have to deal with version collisions.

To explore more about scaling and optimizing Event Sourcing:

How do snapshots work?

When NOT to use Event Sourcing
What are the biggest drawbacks of Event Sourcing, and when should I stick to a traditional CRUD architecture instead?

Event Sourcing is incredibly powerful for complex, high-value business domains, but it is often over-engineered into projects that don't need it. The upfront complexity is high, and the long-term maintenance requires a deep understanding of distributed systems.

Here are the biggest drawbacks of Event Sourcing, followed by a guide on when to stick to a traditional CRUD (Create, Read, Update, Delete) approach.

The Biggest Drawbacks of Event Sourcing
1. The Steep Learning Curve
Event Sourcing requires a complete paradigm shift for developers. Instead of thinking about "what the data looks like right now," developers must think about "what happened in the past to get us here." This shift affects everything from database design to debugging, and onboarding new engineers can take significantly longer.

2. Eventual Consistency (The "UI Lag")
In a traditional CRUD app, you write to the database and read from it immediately. In Event Sourcing paired with CQRS, you write to the Event Store, which publishes to an Event Bus, which updates the Read Database.

This takes time (usually milliseconds, but sometimes longer under load). This means a user might click "Save," refresh the page, and still see their old data for a split second. Designing user interfaces that gracefully handle this eventual consistency is difficult.

3. Schema Evolution is Hard
As discussed with upcasting, changing the shape of your data is no longer a simple ALTER TABLE SQL command. Because the event log is immutable, every structural change you make must account for the entire history of old events. Over years of development, managing these versions becomes a significant architectural burden.

4. Querying the Write Side is Impossible
You cannot easily run an ad-hoc query against an Event Store. If your business team asks, "Give me a list of all users who signed up last week and haven't deposited money," you cannot just write a SELECT statement against the Event Store. You must ensure you have built a specific Read Model (projection) that tracks exactly that data.

5. Infrastructure Complexity
A simple CRUD app needs one database. An Event Sourced app often needs:

An Event Store database (for writes)

A Message Broker / Event Bus (Kafka, RabbitMQ)

A Read database (for fast UI queries)

Worker processes to project events from the bus into the read database.

When to Stick to CRUD
CRUD architecture (state-based data persistence) is the industry standard for a reason: it is fast to build, easy to understand, and well-supported by every framework on earth.

Explore this interactive comparison to see how different business requirements dictate your architectural choices:

Generating interactive visual...
Structuring code, this may take a minute
You should default to CRUD when:
Your domain is a simple "Data Entry" application: If your app is essentially a digital filing cabinet (e.g., a content management system, a simple blog, a basic inventory tracker), Event Sourcing adds no value.

The "Why" doesn't matter: If you only care about a user's current address, and you do not care why or when they moved 3 years ago, use CRUD.

You need strong consistency: If your business rules require absolute, millisecond-perfect consistency across multiple entities at once, CRUD with traditional relational database locks and ACID transactions is much safer.

You have a small team or limited budget: Event sourcing takes longer to build and requires more infrastructure. Startups looking for rapid product-market fit should almost always start with CRUD.

The Golden Rule: Start with CRUD. Only introduce Event Sourcing and CQRS to specific bounded contexts in your application where the complexity of the business logic—or the immense value of the audit trail—justifies the overhead.

How do I migrate from CRUD to Event Sourcing?

Yes
is there a hybrid approach where you write to a message broker which fans out to telemetry, logs, sql server and redis and then when consistency is restored the the queue is empty

Yes, absolutely. This is one of the most popular and pragmatic architectural patterns in modern system design.

It is broadly known as Event-Driven Architecture (EDA), but when you use it specifically to update separate read and write databases without an immutable event log, it is often called Asynchronous CQRS or the Broker-Buffered Write Pattern.

This approach gives you the massive scalability and "fan-out" benefits of Event Sourcing, but lets you keep a traditional CRUD SQL database so you don't have to deal with the complexity of event replays and versioning.

Here is how the flow works, and the one massive "gotcha" you have to watch out for.

The Flow: The Broker as the Front Door
In this model, your API acts as a lightweight bouncer. It doesn't do heavy processing; it just takes the request and throws it onto a message broker (like Kafka, RabbitMQ, or AWS SNS/SQS).

1
API Receives and Queues
Fast and non-blocking
The user submits a payload. The API does basic validation (e.g., "Is this a valid email format?"), immediately pushes a UserUpdateRequested message to the broker, and returns an HTTP 202 Accepted to the user. The API never talks to a database.

2
The Broker Fans Out
The Pub/Sub mechanism
The broker takes that single message and broadcasts it to multiple independent subscriber queues.

3
Workers Consume the Queues
Parallel processing
Independent worker services pull from their respective queues simultaneously:

Worker A: Updates the authoritative state in SQL Server.

Worker B: Updates the fast-read cache in Redis.

Worker C: Formats the data and ships it to DataDog/New Relic for telemetry.

Worker D: Appends the action to ElasticSearch for auditing/logging.

Once all the queues hit exactly zero, the system has reached Eventual Consistency. Redis perfectly matches SQL, the logs are written, and the telemetry is recorded.

The Advantages
Massive Ingestion Scale: Because your API never waits for SQL Server to lock a row or write to a disk, it can handle massive spikes in traffic (like Black Friday sales). The broker just absorbs the spike, and the workers process it at their own pace.

True Decoupling: If Redis goes down, or your Telemetry provider has an outage, the messages just sit safely in their specific queues waiting for the service to come back online. You don't lose data, and your main application doesn't crash.

Simplicity over Event Sourcing: You are just updating rows in SQL and keys in Redis. You don't have to worry about "upcasting" historical events.

The Catch: The "Write-First" Validation Dilemma
If you write directly to the broker first, you run into a major problem with business logic validation.

Imagine a user tries to withdraw $500, but they only have $100 in their SQL database.

The API receives the request, puts it on the queue, and tells the user 202 Accepted (Success!).

A few milliseconds later, Worker A picks up the message, checks SQL Server, and realizes the user is broke. Worker A rejects the transaction.

The problem: You already told the user the withdrawal was accepted.

How to solve this:
If your system requires strict, synchronous business validation (like checking a bank balance), writing to the broker first is dangerous. Instead, the industry uses a hybrid fix called the Transactional Outbox Pattern.

Instead of writing to the broker first:

The API validates the business rules against SQL Server.

The API opens a single database transaction. It updates the user's balance in the Accounts table, AND it writes a message to a special Outbox table in the same SQL database.

The transaction commits. (If either fails, both fail).

A background process constantly reads the Outbox table and instantly pushes those messages to your Message Broker to fan out to Redis, Logs, and Telemetry.

This guarantees your business rules are respected synchronously, but you still get the beautiful, decoupled fan-out for everything else.

Want to dive deeper into the Outbox Pattern?

Yes
Explain the Transactional Outbox pattern in detail. How does it guarantee that messages are never lost even if the system crashes mid-transaction?

To understand why the Transactional Outbox pattern exists, you first have to understand the nightmare it solves: The Dual Write Problem.

Imagine an API that needs to deduct money from a SQL database and then tell a Message Broker to send an email receipt.

If you write code like this:

database.save(newBalance)

messageBroker.publish(receiptEvent)

What happens if the server crashes exactly between Step 1 and Step 2? The money is deducted, but the email is never sent. The system is in an inconsistent state, and because the memory was wiped in the crash, the API has no idea it forgot to send the message.

The Transactional Outbox pattern solves this by leaning on the strongest guarantee in computer science: the relational database ACID transaction.

How the Outbox Pattern Works
Instead of trying to talk to the database and the message broker at the same time, your API only talks to the database.

1
The Single Database Transaction
The ACID Guarantee
The API opens a database transaction. It updates the user's business entity (e.g., updating the Accounts table with the new balance). In the exact same transaction, it inserts a new row into a dedicated Outbox table. This row contains the JSON payload of the event meant for the broker.

2
The Commit
All or Nothing
The database commits the transaction. Because databases are ACID-compliant, this is an "all-or-nothing" operation. If the commit succeeds, both the balance is updated AND the message is safely stored in the Outbox.

3
The Message Relay
A separate background process
A separate worker service (often called the Message Relay or Publisher) constantly watches the Outbox table. It reads the un-sent messages, publishes them to the actual Message Broker (Kafka/RabbitMQ), and then marks the Outbox row as processed (or deletes it).

How It Survives a Mid-Transaction Crash
Let's look at exactly how this guarantees zero message loss when the power is suddenly cut to your API server.

Scenario A: The server crashes BEFORE the database commit finishes.
Because the transaction never fully committed, the database rolls back everything automatically. The account balance is untouched, and the Outbox table is empty. The user will see a failure and retry. The system remains perfectly consistent.

Scenario B: The server crashes AFTER the database commit finishes, but BEFORE the message is published.
This is where the magic happens. The API server is dead, but it doesn't matter. The business state (balance) and the intent to publish (the Outbox row) are safely persisted together on the database's hard drive.
When the Message Relay process runs (which is usually a separate container on a different server), it will eventually see the unpublished row in the Outbox table, pick it up, and send it to the broker. No data is lost.

Explore this interactive diagram to see the architecture and dependency chain in action:

Generating interactive visual...
Structuring code, this may take a minute
The Two Ways to Build the Message Relay
How does the Message Relay actually know when a new row is inserted into the Outbox? There are two ways to do this:

1. Polling (The Easy Way)
You write a simple script that runs SELECT * FROM Outbox WHERE processed = false every 2 seconds. It grabs the rows, sends them, and updates them to processed = true.

Pros: Very easy to write.

Cons: Polling a database every 2 seconds wastes resources and introduces a slight delay in publishing.

2. Change Data Capture / CDC (The Pro Way)
Instead of querying the database, you use a CDC tool like Debezium. Databases like PostgreSQL and MySQL maintain an internal, hidden "Transaction Log" (WAL or Binlog) that records every disk write. Debezium tails this log at the file-system level. The moment the database commits the Outbox row to disk, Debezium instantly streams it to Kafka.

Pros: Near real-time. Zero load on the database CPU.

Cons: Requires setting up and maintaining complex infrastructure like Kafka Connect and Debezium.

The Final Catch: At-Least-Once Delivery
The Outbox pattern guarantees a message is sent at least once, but it does not guarantee exactly-once delivery.

If the Message Relay successfully publishes the message to Kafka, but then the Relay crashes a millisecond before it can update the database to processed = true, what happens? When the Relay restarts, it will see the message is still "unprocessed" and send it a second time.

Because of this, every worker that consumes messages from your broker must be idempotent. They must be designed to safely ignore duplicate messages (e.g., checking "Have I already sent receipt #12345?" before sending the email).