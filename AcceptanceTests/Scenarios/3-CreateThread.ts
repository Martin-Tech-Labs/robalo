import 'chai/register-should.js';
import "../Api/SchemaAssertions.ts";

import { describe, it } from 'mocha';
import { expect } from "chai";

import ControllerApi from '../Api/controllerApi.ts'
import type { Response } from 'supertest';

import type { ServiceDiscovery } from '../Api/Types/service-discovery.d.ts'
import type { Thread } from '../Api/Types/thread.d.ts';
import type { MessageQueryResult } from '../Api/Types/message-query-result.d.ts'
import type { ProblemDetails } from '../Api/Types/problem-details.d.ts'

import { randomUUID } from 'node:crypto';

describe('3 - Create Thread @createThread', () => {
    let serviceDiscovery: ServiceDiscovery;
    let thread: Thread;
    let response: Response;
    let message: string;
    let messageQueryResult: MessageQueryResult;

    describe('Create Thread - Happy Path', () => {

        before(() => {
            message = `msg_${randomUUID().toLowerCase()}`;
        });


        it("Given service discovery document", async () => {
            serviceDiscovery = (await ControllerApi.serviceDiscovery()).body as ServiceDiscovery;
        });


        it(`When I create a new thread with message `, async () => {
            response = await ControllerApi.createThread({ content: message }, serviceDiscovery._links['create-thread']);
        });

        it("Then I verfy the response code", () => {
            response.statusCode.should.equal(201);
        });


        it("And I verfy the content type application/hal+json", () => {
            response.headers['content-type'].should.include('application/hal+json');
        });

        it("And I verfy the schema", () => {
            expect(response.body).to.have.validThreadSchema();
            thread = response.body as Thread;

        });

        it("And I verify the location link matches the self link", () => {
            response.headers['location'].should.equal(thread._links.self.href);
        });

        it("When I go to the self link", async () => {
            response = await ControllerApi.get(thread._links.self);
        });

        it("Then I should get the valid response code 200", async () => {
            response.statusCode.should.equal(200);
        });

        it("And I verfy the content type application/hal+json", () => {
            response.headers['content-type'].should.include('application/hal+json');
        });

        it("And I verfy the schema", () => {
            expect(response.body).to.have.validThreadSchema();
        });

        it("And I verfy the resources match", () => {
            let threadSelf = response.body as Thread;
            thread.should.deep.equal(threadSelf);
        });

        it("When I navigate to messages link", async () => {
            response = await ControllerApi.get(thread._links.messages);
        });

        it("Then I should get the valid response code 200", async () => {
            response.statusCode.should.equal(200);
        });

        it("And I verfy the content type application/hal+json", () => {
            response.headers['content-type'].should.include('application/hal+json');
        });

        it("And I verfy the schema", () => {
            expect(response.body).to.have.validMessageQueryResultSchema();
        });

        it("And I verfy the message", () => {
            messageQueryResult = response.body as MessageQueryResult;
            expect(messageQueryResult._embedded.messages.length).to.equal(1);
            expect(messageQueryResult._embedded.messages[0].content).to.equal(message);
            expect(messageQueryResult._embedded.messages[0].source).to.equal("user");
            expect(messageQueryResult._embedded.messages[0].message_number).to.equal(1);
            expect(messageQueryResult._embedded.messages[0].created_on).to.equal(thread.created_on);
        });

        it("And I verfy next and prev links are null", () => {
            expect(messageQueryResult._links.next).to.be.undefined;
            expect(messageQueryResult._links.prev).to.be.undefined;
        });
    })


    describe("Create Thread - Missing Message", () => {
        let problemDetails: ProblemDetails;
        
        before(() => {
            message = "";
        });


        it("Given service discovery document", async () => {
            serviceDiscovery = (await ControllerApi.serviceDiscovery()).body as ServiceDiscovery;
        });


        it(`When I make a post request to the create-thread with missing message `, async () => {
            response = await ControllerApi.createThread({ content: message }, serviceDiscovery._links['create-thread']);
        });

        it("Then I should get 422", () => {
            response.statusCode.should.equal(422);
        });

        it("And I verfy the content type application/problem+json", () => {
            response.headers['content-type'].should.include('application/problem+json');
        });

        it("And I verfy the schema", () => {
            expect(response.body).to.have.validProblemDetailsSchema();
        });

        it("And I verfy the problem details", () => {
            problemDetails = response.body as ProblemDetails;
            problemDetails.title.should.equal("Validation failed");
            problemDetails.detail.should.equal("content_must_be_provided");
            problemDetails.status.should.equal(422);
        });
    })

    describe("Create Thread - Malformed payload", () => {
        let problemDetails: ProblemDetails;


        it("Given service discovery document", async () => {
            serviceDiscovery = (await ControllerApi.serviceDiscovery()).body as ServiceDiscovery;
        });


        it(`When I create a new thread with message `, async () => {
            response = await ControllerApi.post("{{≠}ºls", serviceDiscovery._links['create-thread']);
        });

        it("Then I should get 400", () => {
            response.statusCode.should.equal(400);
        });

        it("And I verfy the content type application/problem+json", () => {
            response.headers['content-type'].should.include('application/problem+json');
        });

        it("And I verfy the schema", () => {
            expect(response.body).to.have.validProblemDetailsSchema();
        });

        it("And I verfy the problem details", () => {
            problemDetails = response.body as ProblemDetails;
            problemDetails.title.should.equal("Malformed request body.");
            problemDetails.detail.should.equal("invalid_json");
            problemDetails.status.should.equal(400);
        });
    })
});