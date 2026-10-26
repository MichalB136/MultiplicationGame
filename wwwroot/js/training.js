(() => {
    const storageKey = "multiplication-game-progress-v1";
    const storageMessage = document.getElementById("trainingStorageMessage");
    const setupForm = document.getElementById("trainingSetupForm");
    const tableCheckboxes = setupForm?.querySelectorAll('input[name="SelectedTables"]');
    const knowledgeInput = document.getElementById("trainingKnowledge");
    const adaptiveMode = document.getElementById("adaptiveMode");
    const totalsElement = document.getElementById("trainingProgressTotals");
    const weakFactsElement = document.getElementById("trainingWeakFacts");
    const clearProgressButton = document.getElementById("clearTrainingProgress");

    function createEmptyProgress() {
        return {
            Version: 1,
            Facts: {},
            RecordedAnswers: []
        };
    }

    function readProgress() {
        const raw = localStorage.getItem(storageKey);
        if (raw === null) {
            return createEmptyProgress();
        }

        let progress;
        try {
            progress = JSON.parse(raw);
        } catch (error) {
            if (error instanceof SyntaxError) {
                throw new Error("Zapisane wyniki mają nieprawidłowy format.");
            }

            throw error;
        }

        if (!progress ||
            progress.Version !== 1 ||
            !progress.Facts ||
            typeof progress.Facts !== "object" ||
            Array.isArray(progress.Facts) ||
            !Array.isArray(progress.RecordedAnswers)) {
            throw new Error("Zapisane wyniki mają nieobsługiwaną strukturę.");
        }

        for (const [key, fact] of Object.entries(progress.Facts)) {
            if (!/^(?:[1-9]|10)-(?:[1-9]|10)$/.test(key) ||
                !fact ||
                !Number.isInteger(fact.Attempts) ||
                !Number.isInteger(fact.Correct) ||
                !Number.isInteger(fact.AssistedAnswers) ||
                fact.Attempts < 0 ||
                fact.Correct < 0 ||
                fact.Correct > fact.Attempts ||
                fact.AssistedAnswers < 0 ||
                fact.AssistedAnswers > fact.Attempts) {
                throw new Error("Zapisane wyniki zawierają nieprawidłowe dane.");
            }
        }

        return progress;
    }

    function showStorageMessage(message) {
        if (!storageMessage) {
            return;
        }

        storageMessage.textContent = message;
        storageMessage.classList.remove("d-none");
    }

    function renderProgress(progress) {
        if (!totalsElement || !weakFactsElement) {
            return;
        }

        const facts = Object.entries(progress.Facts);
        const attempts = facts.reduce((total, [, fact]) => total + fact.Attempts, 0);
        const correct = facts.reduce((total, [, fact]) => total + fact.Correct, 0);
        const assisted = facts.reduce((total, [, fact]) => total + fact.AssistedAnswers, 0);

        if (attempts === 0) {
            totalsElement.textContent = "Brak zapisanych odpowiedzi. Wyniki pojawią się po pierwszej rundzie.";
            weakFactsElement.replaceChildren();
            return;
        }

        const accuracy = Math.round((correct / attempts) * 100);
        totalsElement.textContent =
            `Odpowiedzi: ${attempts} · Poprawność: ${accuracy}% · Z podpowiedzią: ${assisted}`;

        const weakestFacts = facts
            .filter(([, fact]) => fact.Attempts > 0)
            .sort((left, right) =>
                ((right[1].Attempts - right[1].Correct) / right[1].Attempts) -
                ((left[1].Attempts - left[1].Correct) / left[1].Attempts))
            .slice(0, 3);

        weakFactsElement.replaceChildren();
        for (const [key, fact] of weakestFacts) {
            const item = document.createElement("li");
            item.className = "list-group-item px-0 py-1";
            item.textContent =
                `${key.replace("-", " × ")} · błędne odpowiedzi: ${fact.Attempts - fact.Correct}/${fact.Attempts}`;
            weakFactsElement.appendChild(item);
        }
    }

    function recordAnswers(progress) {
        const events = document.querySelectorAll(".training-answer-event");
        let changed = false;

        for (const event of events) {
            const answerId = `${event.dataset.roundId}-${event.dataset.index}`;
            if (progress.RecordedAnswers.includes(answerId)) {
                continue;
            }

            const factKey = `${event.dataset.a}-${event.dataset.b}`;
            const fact = progress.Facts[factKey] ?? {
                Attempts: 0,
                Correct: 0,
                AssistedAnswers: 0
            };

            fact.Attempts++;
            if (event.dataset.correct === "true") {
                fact.Correct++;
            }
            if (event.dataset.assisted === "true") {
                fact.AssistedAnswers++;
            }

            progress.Facts[factKey] = fact;
            progress.RecordedAnswers.push(answerId);
            changed = true;
        }

        if (progress.RecordedAnswers.length > 5000) {
            progress.RecordedAnswers = progress.RecordedAnswers.slice(-5000);
        }

        if (changed) {
            localStorage.setItem(storageKey, JSON.stringify(progress));
        }
    }

    let progress = null;
    try {
        progress = readProgress();
        recordAnswers(progress);
        renderProgress(progress);
        if (knowledgeInput) {
            knowledgeInput.value = JSON.stringify(progress.Facts);
        }
    } catch {
        showStorageMessage(
            "Nie można odczytać lub zapisać postępów w tej przeglądarce. Trening standardowy nadal działa.");
    }

    if (setupForm && knowledgeInput) {
        if (tableCheckboxes?.length) {
            const firstTableCheckbox = tableCheckboxes[0];
            const updateTableValidity = () => {
                const hasSelection = Array.from(tableCheckboxes).some(checkbox => checkbox.checked);
                firstTableCheckbox.setCustomValidity(
                    hasSelection ? "" : "Wybierz co najmniej jedną tabliczkę od 1 do 10.");
            };

            tableCheckboxes.forEach(checkbox => {
                checkbox.addEventListener("change", updateTableValidity);
            });
            updateTableValidity();
        }

        setupForm.addEventListener("submit", event => {
            try {
                const currentProgress = readProgress();
                knowledgeInput.value = JSON.stringify(currentProgress.Facts);
            } catch {
                if (adaptiveMode?.checked) {
                    event.preventDefault();
                    showStorageMessage(
                        "Tryb adaptacyjny wymaga dostępnych, poprawnych danych zapisanych w przeglądarce.");
                    return;
                }

                knowledgeInput.value = "{}";
            }
        });
    }

    if (clearProgressButton) {
        clearProgressButton.addEventListener("click", () => {
            try {
                localStorage.removeItem(storageKey);
                progress = createEmptyProgress();
                if (knowledgeInput) {
                    knowledgeInput.value = "{}";
                }
                if (storageMessage) {
                    storageMessage.classList.add("d-none");
                }
                renderProgress(progress);
            } catch {
                showStorageMessage("Nie udało się wyczyścić postępów w tej przeglądarce.");
            }
        });
    }

    const hint = document.getElementById("trainingHint");
    const usedHint = document.getElementById("usedHint");
    if (hint && usedHint) {
        const markHintAsUsed = () => {
            if (hint.open) {
                usedHint.value = "true";
            }
        };

        hint.addEventListener("toggle", markHintAsUsed);
        markHintAsUsed();
    }
})();
