export interface RetryOptions {
    initTimeout?: number;
    maxAttempts?: number;
    timeoutBetweenRetries?: number;
}

const wait = (milliseconds: number) =>
    new Promise<void>(resolve => setTimeout(resolve, milliseconds));

export function retry<T>(action: () => Promise<T>, options: RetryOptions = {}): () => Promise<T> {
    const {
        initTimeout = 0,
        maxAttempts = 10,
        timeoutBetweenRetries = 1000,
    } = options;

    return async () => {
        await wait(initTimeout);

        for (let attempt = 1; attempt <= maxAttempts; attempt++) {
            try {
                return await action();
            } catch (error) {
                if (attempt === maxAttempts) {
                    throw error;
                }
                console.info(
                    `Attempt ${attempt}/${maxAttempts} failed; retrying in ${timeoutBetweenRetries} ms.`);
                await wait(timeoutBetweenRetries);
            }
        }

        throw new Error('Retry completed without an attempt.');
    };
}
