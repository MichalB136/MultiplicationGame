# Application processes and flows

These diagrams describe the current Razor Pages implementation.

## Choosing a game mode

```mermaid
flowchart TD
    Start["Open the application"] --> Home["Home: mixed multiplication"]
    Start --> Training["/Training: choose or continue training"]
    Start --> Equations["/Equations: arithmetic equations"]

    Home --> HomePlay["Answer questions and track streaks and lives"]
    Training --> TrainingSetup["Choose tables and optional adaptive mode or hints"]
    Equations --> EquationPlay["Choose difficulty and answer equations"]
```

## Training round

```mermaid
flowchart TD
    Open["Open /Training"] --> HasRound{"Active round in ASP.NET session?"}
    HasRound -- Yes --> RoundComplete{"Round complete?"}
    RoundComplete -- Yes --> Summary["Show score, time, and answers"]
    RoundComplete -- No --> Question["Show current question"]
    HasRound -- No --> Setup["Choose one or more tables"]

    Setup --> HasTable{"At least one table selected?"}
    HasTable -- No --> BrowserPrompt["Browser shows native validation prompt"]
    BrowserPrompt --> Setup
    HasTable -- Yes --> Adaptive{"Adaptive mode enabled?"}

    Adaptive -- Yes --> ReadProgress["Read fact statistics from localStorage"]
    ReadProgress --> SendProgress["Submit selected tables and progress JSON"]
    Adaptive -- No --> Standard["Submit selected tables"]

    SendProgress --> Validate["Server validates tables and optional progress"]
    Standard --> Validate
    Validate --> SetupValid{"Setup data valid?"}
    SetupValid -- No --> SetupError["Return setup validation error"]
    SetupError --> Setup
    SetupValid -- Yes --> Generate["Generate 10 questions and save round in ASP.NET session"]
    Generate --> Question

    Question --> Answer["Enter an integer answer"]
    Answer --> ValidAnswer{"Required value and integer format valid?"}
    ValidAnswer -- No --> AnswerPrompt["Browser shows native validation prompt"]
    AnswerPrompt --> Answer
    ValidAnswer -- Yes --> PostAnswer["POST answer to the server"]
    PostAnswer --> AnswerValid{"Integer answer valid on server?"}
    AnswerValid -- No --> AnswerError["Return validation error"]
    AnswerError --> Answer
    AnswerValid -- Yes --> SaveAnswer["Save answer in session"]
    SaveAnswer --> Correct{"Answer correct?"}
    Correct -- No --> AddMistake["Add question to the round's mistakes"]
    Correct -- Yes --> Count["Count answer"]
    AddMistake --> Count
    Count --> Record["training.js records the answer in localStorage"]
    Record --> Complete{"All 10 questions answered?"}
    Complete -- No --> Question
    Complete -- Yes --> Summary
    Summary --> Repeat{"Repeat mistakes?"}
    Repeat -- Yes --> RepeatRound["Start a round containing only mistakes"]
    RepeatRound --> Question
    Repeat -- No --> New["Choose another training"]
    New --> Setup
```

## Progress data

```mermaid
sequenceDiagram
    actor Player
    participant Browser
    participant Server as ASP.NET app
    participant Session as ASP.NET session
    participant Storage as Browser localStorage

    Player->>Browser: Start a training round
    opt Adaptive mode enabled
        Browser->>Storage: Read per-fact statistics
        Storage-->>Browser: Fact attempts, correct answers, and hint usage
        Browser->>Server: Submit selected tables and statistics
        Server->>Server: Validate statistics and select questions
    end
    Server->>Session: Save current questions and answers
    Server-->>Browser: Redirect to the active round

    loop For each valid answer
        Player->>Browser: Submit integer answer
        Browser->>Server: POST answer
        Server->>Session: Save answer and mistake state
        Server-->>Browser: Redirect to updated round
        Browser->>Storage: Record answer once using round ID and answer index
    end

    Player->>Browser: Clear saved progress
    Browser->>Storage: Remove training statistics
```

## Equations round

```mermaid
flowchart TD
    Open["Open /Equations"] --> Difficulty["Select difficulty"]
    Difficulty --> Question["Generate an arithmetic equation"]
    Question --> Answer["Enter an answer"]
    Answer --> Valid{"Required value and supported number format valid?"}
    Valid -- No --> Prompt["Browser shows native validation prompt"]
    Prompt --> Answer
    Valid -- Yes --> Submit["Submit answer"]
    Submit --> Evaluate["Server parses and evaluates the answer"]
    Evaluate --> History["Add result to the round history"]
    History --> Finished{"20 answers recorded?"}
    Finished -- No --> Result["Show result and next-question action"]
    Result --> Question
    Finished -- Yes --> Summary["Show round summary"]
    Summary --> Restart["Start another round"]
    Restart --> Difficulty
```

## Home mixed multiplication

```mermaid
flowchart TD
    Open["Open /"] --> Question["Show multiplication question"]
    Question --> Answer["Enter an integer answer"]
    Answer --> Valid{"Required value and integer format valid?"}
    Valid -- No --> Prompt["Browser shows native validation prompt"]
    Prompt --> Answer
    Valid -- Yes --> Post["Submit answer"]
    Post --> Evaluate["Server evaluates answer and updates streak, lives, and history"]
    Evaluate --> Continue{"Game continues?"}
    Continue -- Yes --> Question
    Continue -- No --> Summary["Show game result"]
```
