import 'chai/register-should.js';
import "../Api/SchemaAssertions.ts";

import { describe, it } from 'mocha';
import { expect } from "chai";

import ControllerApi from '../Api/controllerApi.ts'
import type { Response } from 'supertest';

describe('2 - Service Discovery @serviceDiscovery', () => {
    describe('Service Discovery endpoint', () => {

        let response: Response;
        it("When I get /", async () => { response = await ControllerApi.serviceDiscovery() });

        it("Then I verify the response code is 200", () => { response.statusCode.should.equal(200) });

        it("And I verfy the content type application/hal+json", () => {
            response.headers['content-type'].should.include('application/hal+json');
        });

        it("And I verfy the schema", () => {
            expect(response.body).to.have.validServiceDiscoverySchema();
        });
    })
});