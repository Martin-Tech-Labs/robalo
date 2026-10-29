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
import type { Message } from '../Api/Types/message.d.ts'

import { randomUUID } from 'node:crypto';
import { retry } from '../Api/retry.ts';

describe('3 - Threads @drei @threads', () => {
    let serviceDiscovery: ServiceDiscovery;
    let thread: Thread;
    let response: Response;
    let message1Content: string;
    let message2Content: string;
    let message: Message;
    let messageQueryResult: MessageQueryResult;

    describe('Create Thread - Happy Path @threads @thread', () => {
        before(() => {
            message1Content = `msg_${randomUUID().toLowerCase()}`;
            message2Content = `msg_${randomUUID().toLowerCase()}`;
        });

        it("Given service discovery document", async () => {
            serviceDiscovery = (await ControllerApi.serviceDiscovery()).body as ServiceDiscovery;
        });


        it(`When I create a new thread with message `, async () => {
            response = await ControllerApi.post({ content: message1Content }, serviceDiscovery._links.threads);
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

        it("And I verify the thread modified_on equals created_on", () => {
            thread.created_on.should.equal(thread.modified_on);
        });

        it("And I verify the thread title is not present", () => {
            expect(thread.title).to.be.undefined;
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

        it("And I verify the resources match", () => {
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
            expect(messageQueryResult._embedded.messages.length).to.be.greaterThan(0);
            expect(messageQueryResult._embedded.messages[0].content).to.equal(message1Content);
            expect(messageQueryResult._embedded.messages[0].source).to.equal("user");
            expect(messageQueryResult._embedded.messages[0].message_number).to.equal(1);
            expect(messageQueryResult._embedded.messages[0].created_on).to.equal(thread.created_on);
        });

        it("And I wait for the LLM response", retry(async () => {
            response = await ControllerApi.get(thread._links.messages);
            messageQueryResult = response.body as MessageQueryResult;
            expect(messageQueryResult._embedded.messages.length).to.equal(2);
        }
        )).timeout(10000);

        it("And I verfy the LLM response", () => {
            expect(messageQueryResult._embedded.messages[1].content).to.not.be.empty;
            expect(messageQueryResult._embedded.messages[1].source).to.equal("llm");
            expect(messageQueryResult._embedded.messages[1].message_number).to.equal(2);
        });

        it("And I verfy next and prev links are null", () => {
            expect(messageQueryResult._links.next).to.be.undefined;
            expect(messageQueryResult._links.prev).to.be.undefined;
        });

        it("When I add one more message to a thread", async () => {
            response = await ControllerApi.post({ content: message2Content }, thread._links.messages);
        });

        it("Then I verfy the response code", () => {
            response.statusCode.should.equal(201);
        });


        it("And I verfy the content type application/hal+json", () => {
            response.headers['content-type'].should.include('application/hal+json');
        });

        it("And I verfy the schema", () => {
            expect(response.body).to.have.validMessageSchema();
            message = response.body as Message;
        });

        it("And I verify the location link matches the self link", () => {
            response.headers['location'].should.equal(message._links.self.href);
        });

        it("And I verify the thread link matches the thread self link", () => {
            message._links.thread.href.should.equal(thread._links.self.href);
        });

        it("And I verify the message content is valid", () => {
            message.content.should.equal(message2Content);
        });

        it("And I verify the message thread id is valid", () => {
            message.thread_id.should.equal(thread.thread_id);
        });

        it("And I verify the message source is valid", () => {
            message.source.should.equal("user");
        });

        it("And I verify the created on is valid", () => {
            Date.parse(message.created_on).should.be.greaterThan(Date.parse(thread.created_on));
        });

        it("And I verify the message number is 3", () => {
            message.message_number.should.equal(3);
        });

        it("When I go to message self link", async () => {
            response = await ControllerApi.get(message._links.self);
        });

        it("Then I verify status code", () => {
            response.statusCode.should.equal(200);
        });

        it("And I verify the schema", () => {
            expect(response.body).to.be.validMessageSchema();
        });

        it("And I verify the retrieved message via self links matches the created response", () => {
            expect(response.body as Message).to.deep.equal(message);
        });

        it("When I reload the thread by going to its self link", async () => {
            response = await ControllerApi.get(thread._links.self).expect(200);
        });

        it("Then I verify modified_on is updated to that of the last message", async () => {
            let resultSelf = response.body as Thread;
            resultSelf.created_on.should.equal(thread.created_on);
            resultSelf.modified_on.should.equal(message.created_on);
        });

        it("And I wait for the LLM response", retry(async () => {
            response = await ControllerApi.get(thread._links.messages);
            messageQueryResult = response.body as MessageQueryResult;
            expect(messageQueryResult._embedded.messages.length).to.equal(4);
        }
        )).timeout(10000);

        it("And I verfy the LLM response", () => {
            expect(messageQueryResult._embedded.messages[3].content).to.not.be.empty;
            expect(messageQueryResult._embedded.messages[3].source).to.equal("llm");
            expect(messageQueryResult._embedded.messages[3].message_number).to.equal(4);
        });

        it("When I reload the thread by going to its self link", async () => {
            response = await ControllerApi.get(thread._links.self).expect(200);
        });

        it("Then I verify modified_on is updated to that of the last message", async () => {
            let resultSelf = response.body as Thread;
            resultSelf.created_on.should.equal(thread.created_on);
            resultSelf.modified_on.should.equal(messageQueryResult._embedded.messages[3].created_on);
        });

        it("When I delete the thread", async () => {
            response = await ControllerApi.delete(thread._links.self);
        })

        it("Then I verify the valid response code 204", async () => {
            response.statusCode.should.equal(204);
        });

        it("When I try to retrive the thread using previously saved self link", async () => {
            response = await ControllerApi.get(thread._links.self);
        })

        it("Then I verify the valid response code 404", async () => {
            response.statusCode.should.equal(404);
        });

        it("When I try to delete the thread again using previously saved self link", async () => {
            response = await ControllerApi.delete(thread._links.self);
        })

        it("Then I verify the valid response code 404", async () => {
            response.statusCode.should.equal(404);
        });
    })


    describe("Create Thread - Missing Message", () => {
        let problemDetails: ProblemDetails;

        before(() => {
            message1Content = "";
        });


        it("Given service discovery document", async () => {
            serviceDiscovery = (await ControllerApi.serviceDiscovery()).body as ServiceDiscovery;
        });


        it(`When I make a post request to the create-thread with missing message `, async () => {
            response = await ControllerApi.post({ content: message1Content }, serviceDiscovery._links.threads);
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
            response = await ControllerApi.post("{{≠}ºls", serviceDiscovery._links.threads);
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

    describe("Add Message - Missing Content", () => {
        let problemDetails: ProblemDetails;
        let thread: Thread;


        it("Given service discovery document", async () => {
            serviceDiscovery = (await ControllerApi.serviceDiscovery()).body as ServiceDiscovery;
        });


        it(`And existing thread`, async () => {
            response = await ControllerApi.post({ content: "test" }, serviceDiscovery._links.threads).expect(201);
            thread = response.body as Thread;
        });

        it("When I add a message to it with missing content", async () => {
            response = await ControllerApi.post({ content: "" }, thread._links.messages);
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

    describe("Add message - Malformed payload", () => {
        let thread: Thread;
        let problemDetails: ProblemDetails;

        it("Given service discovery document", async () => {
            serviceDiscovery = (await ControllerApi.serviceDiscovery()).body as ServiceDiscovery;
        });

        it(`And an existing thread`, async () => {
            response = await ControllerApi.post({ content: "Test" }, serviceDiscovery._links.threads).expect(201);
            thread = response.body as Thread;
        });

        it("When I add a new message with invalid json", async () => {
            response = await ControllerApi.post("{{≠}ºls", thread._links.messages);
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
