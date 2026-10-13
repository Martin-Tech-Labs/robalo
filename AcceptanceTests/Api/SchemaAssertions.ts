declare global {
    namespace Chai {
        interface Assertion {
            validServiceDiscoveryScheme(message?: string): Assertion;
        }
    }
}

import { use } from "chai";

import { Ajv } from "ajv";

import formats from "ajv-formats";

import $RefParser from "@apidevtools/json-schema-ref-parser";

const ajv = new Ajv()
formats.default(ajv);

const validateServiceDiscoveryScheme = ajv.compile((await $RefParser.dereference("specs/controller/schemas/service-discovery.yaml")));

use((chai, utils) => {
    utils.addChainableMethod(
        chai.Assertion.prototype,
        "validServiceDiscoveryScheme" satisfies keyof Chai.Assertion,
        function (this: Chai.AssertionPrototype, message?: string) {
            const obj = utils.flag(this, "object");

            const isValid = validateServiceDiscoveryScheme(obj);
            this.assert(
                isValid,
               `expected #{this} to match ServiceDiscovery schema: ${ajv.errorsText(validateServiceDiscoveryScheme.errors)}${message ? `: ${message}` : ""}`,
                "expected #{this} to not match valid ServiceDiscovery schema" + (message ? ": " + message : "")
            );
        },
    );
});