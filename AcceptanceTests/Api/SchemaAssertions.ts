declare global {
    namespace Chai {
        interface Assertion {
            validServiceDiscoverySchema(message?: string): Assertion;
            validThreadSchema(message?: string): Assertion;
            validThreadListSchema(message?: string): Assertion;
            validMessageQueryResultSchema(message?: string): Assertion;
            validProblemDetailsSchema(message?: string): Assertion;
            validMessageSchema(message?: string): Assertion;
        }
    }
}

import { use } from "chai";

import { Ajv, type ValidateFunction } from "ajv";

import formats from "ajv-formats";

import $RefParser from "@apidevtools/json-schema-ref-parser";

const ajv = new Ajv()
formats.default(ajv);
ajv.addKeyword("externalDocs");

let validateServiceDiscoverySchema: ValidateFunction;
let validateThreadSchema: ValidateFunction;
let validateMessageQueryResultSchema: ValidateFunction;
let validateProblemDetailsSchema: ValidateFunction;
let validateThreadListSchema: ValidateFunction;
let validateMessageSchema: ValidateFunction;

before(async () => {
    validateServiceDiscoverySchema = ajv.compile((await $RefParser.dereference("specs/controller/schemas/service-discovery.yaml")));
    validateThreadSchema = ajv.compile((await $RefParser.dereference("specs/controller/schemas/thread.yaml")));
    validateThreadListSchema = ajv.compile((await $RefParser.dereference("specs/controller/schemas/thread-list.yaml")));
    validateMessageQueryResultSchema = ajv.compile((await $RefParser.dereference("specs/controller/schemas/message-query-result.yaml")));
    validateProblemDetailsSchema = ajv.compile((await $RefParser.dereference("specs/controller/schemas/problem-details.yaml")));
    validateMessageSchema = ajv.compile((await $RefParser.dereference("specs/controller/schemas/message.yaml")));

});

use((chai, utils) => {
    utils.addChainableMethod(
        chai.Assertion.prototype,
        "validThreadSchema" satisfies keyof Chai.Assertion,
        function (this: Chai.AssertionPrototype, message?: string) {
            const obj = utils.flag(this, "object");

            const isValid = validateThreadSchema(obj);
            this.assert(
                isValid,
                `expected #{this} to match Thread schema: ${ajv.errorsText(validateThreadSchema.errors)}${message ? `: ${message}` : ""}`,
                "expected #{this} to not match valid Thread schema" + (message ? ": " + message : "")
            );
        },
    );

    utils.addChainableMethod(
        chai.Assertion.prototype,
        "validThreadListSchema" satisfies keyof Chai.Assertion,
        function (this: Chai.AssertionPrototype, message?: string) {
            const obj = utils.flag(this, "object");

            const isValid = validateThreadListSchema(obj);
            this.assert(
                isValid,
                `expected #{this} to match ThreadList schema: ${ajv.errorsText(validateThreadListSchema.errors)}${message ? `: ${message}` : ""}`,
                "expected #{this} to not match valid ThreadList schema" + (message ? ": " + message : "")
            );
        },
    );

    utils.addChainableMethod(
        chai.Assertion.prototype,
        "validMessageSchema" satisfies keyof Chai.Assertion,
        function (this: Chai.AssertionPrototype, message?: string) {
            const obj = utils.flag(this, "object");

            const isValid = validateMessageSchema(obj);
            this.assert(
                isValid,
                `expected #{this} to match Message schema: ${ajv.errorsText(validateMessageSchema.errors)}${message ? `: ${message}` : ""}`,
                "expected #{this} to not match valid Message schema" + (message ? ": " + message : "")
            );
        },
    );

    utils.addChainableMethod(
        chai.Assertion.prototype,
        "validServiceDiscoverySchema" satisfies keyof Chai.Assertion,
        function (this: Chai.AssertionPrototype, message?: string) {
            const obj = utils.flag(this, "object");

            const isValid = validateServiceDiscoverySchema(obj);
            this.assert(
                isValid,
                `expected #{this} to match ServiceDiscovery schema: ${ajv.errorsText(validateServiceDiscoverySchema.errors)}${message ? `: ${message}` : ""}`,
                "expected #{this} to not match valid ServiceDiscovery schema" + (message ? ": " + message : "")
            );
        },
    );

    utils.addChainableMethod(
        chai.Assertion.prototype,
        "validMessageQueryResultSchema" satisfies keyof Chai.Assertion,
        function (this: Chai.AssertionPrototype, message?: string) {
            const obj = utils.flag(this, "object");

            const isValid = validateMessageQueryResultSchema(obj);
            this.assert(
                isValid,
                `expected #{this} to match MessageQueryResult schema: ${ajv.errorsText(validateMessageQueryResultSchema.errors)}${message ? `: ${message}` : ""}`,
                "expected #{this} to not match valid MessageQueryResult schema" + (message ? ": " + message : "")
            );
        },
    );

    utils.addChainableMethod(
        chai.Assertion.prototype,
        "validProblemDetailsSchema" satisfies keyof Chai.Assertion,
        function (this: Chai.AssertionPrototype, message?: string) {
            const obj = utils.flag(this, "object");

            const isValid = validateProblemDetailsSchema(obj);
            this.assert(
                isValid,
                `expected #{this} to match ProblemDetails schema: ${JSON.stringify(validateProblemDetailsSchema.errors, null, 2)}${message ? `: ${message}` : ""}`,
                "expected #{this} to not match valid ProblemDetails schema" + (message ? ": " + message : "")
            );
        },
    );
});