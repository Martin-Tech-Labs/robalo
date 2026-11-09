import 'chai/register-should.js';
import "../Api/SchemaAssertions.ts";

import { describe, it } from 'mocha';
import { expect } from "chai";

import ControllerApi from '../Api/controllerApi.ts'
import type { Response } from 'supertest';
import type { ServiceDiscovery } from '../Api/Types/service-discovery.d.ts'

describe('6 - Configuration @configuration @root', () => {
    describe('Configuration endpoint', () => {

        let response: Response;
        let serviceDiscovery: ServiceDiscovery;
        
        it("Given service discovery document", async () => {
            serviceDiscovery = (await ControllerApi.serviceDiscovery()).body as ServiceDiscovery;
        });
        it("When I get configuration", async () => { response = await ControllerApi.get(serviceDiscovery._links.configuration) });

        it("Then I verify the response code is 200", () => { response.statusCode.should.equal(200) });

        it("And I verfy the content type application/hal+json", () => {
            response.headers['content-type'].should.include('application/hal+json');
        });

        it("And I verfy the schema", () => {
            expect(response.body).to.have.validConfigurationSchema();
        });

        it("And I verify the llm key is hidden", () => expect(response.body.data.Llm.key).to.equal("<hidden>"));
    })
});