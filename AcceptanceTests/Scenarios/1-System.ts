import 'chai/register-should.js';
import { describe, it } from 'mocha';

import ControllerApi from '../Api/controllerApi.ts'
import type { Response } from 'supertest';

describe('1 - Scenario: System Endpoints', () => {
    describe('Ping endpoint', () => {

        let response: Response;
        it("When I get /_system/ping", async () => { response = await ControllerApi.ping() });

        it("Then I verify the response code is 200", () => { response.statusCode.should.equal(200) });

        it("And I verfy the content type istext/plain", () => {
            response.headers['content-type'].should.include('text/plain');
        });

        it('And I verify the response body is ping', () => { response.text.should.equal("pdong") });
    })

    describe('Health endpoint', () => {

        let response: Response;
        it("When I get /_system/health", async () => { response = await ControllerApi.health() });

        it("Then I verify the response code is 200", () => { response.statusCode.should.equal(200) });

        it("And I verfy the content type is application/json", () => {
            response.headers['content-type'].should.include('application/json');
        });

        it("And I very the response body lists expected health checks", () => {
            response.body.should.deep.equal({
                "Test Another": "OK",
                "Test One": "OK"
            });
        });
    });

    describe('Metrics endpoint', () => {
        let response: Response;
        it("When I get /_system/metrics", async () => { response = await ControllerApi.metrics() });

        it("Then I verify the response code is 200", () => { response.statusCode.should.equal(200) });

        it("And I verfy the content type is text/plain", () => {
            response.headers['content-type'].should.include('text/plain');
        });

        it("And I verify the response body contains valid metrics details ", () => {
            response.text.should.match(/^kestrel_active_connections(?:\{[^}]*\})?\s+\d+(?:\.\d+)?$/m);
        });
    });

    describe('Env endpoint', () => {
        let response: Response;
        it("When I get /_system/env", async () => { response = await ControllerApi.env() });

        it("Then I verify the response code is 200", () => { response.statusCode.should.equal(200) });

        it("And I verfy the content type is application/json", () => {
            response.headers['content-type'].should.include('application/json');
        });

        it("And I verify the response body contains valid details", () => {
            response.body.application_name.should.equal("Robalo.Controller.Api");
            response.body.version.should.not.be.empty;
            response.body.os.should.not.be.empty;
            response.body.os.should.not.be.empty;
            response.body.machine.should.not.be.empty;
            response.body.environment.should.not.be.empty;
            response.body.runtime.should.include(".NET 11");
            response.body.running_in_container.should.not.be.undefined;
            response.body.uptime_seconds.should.be.above(0);
        });
    });
});