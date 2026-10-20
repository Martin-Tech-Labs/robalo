#!/bin/sh
set -e


model="smollm2:135m-instruct-q4_K_M"

for attempt in 1 2 3 4 5 6 7 8 9 10; do
    if ollama list >/dev/null 2>&1; then
        echo "Ollama setup: $attempt 1 out of 10"
        ollama pull $model
        ollama cp $model test-model
        echo "Ollama setup complete"
        exit 0
    fi
    sleep 1
done

echo "Ollama did not become ready after 10 attempts." >&2
exit 1
