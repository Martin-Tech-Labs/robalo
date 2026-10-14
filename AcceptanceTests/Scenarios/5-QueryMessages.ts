import 'chai/register-should.js';
import "../Api/SchemaAssertions.ts";

import { describe, it } from 'mocha';
import { expect } from "chai";

import ControllerApi from '../Api/controllerApi.ts'
import type { Response } from 'supertest';

import type { ServiceDiscovery } from '../Api/Types/service-discovery.d.ts'
import type { Thread } from '../Api/Types/thread.d.ts';
import type { Message } from '../Api/Types/message.d.ts';
import type { MessageQueryResult } from '../Api/Types/message-query-result.d.ts'
import { resourceLimits } from 'node:worker_threads';



describe('5 - Query Messages @queryThreads @threads', () => {
    let serviceDiscovery: ServiceDiscovery;
    let thread: Thread;
    let response: Response;
    let result: MessageQueryResult;
    let limit = 10;
    let numberOfMessages = limit * 3 + 1; // 31

    describe('Query Threads', () => {
        before(() => {
        });

        it("Given service discovery document", async () => {
            serviceDiscovery = (await ControllerApi.serviceDiscovery().expect(200)).body as ServiceDiscovery;
        });

        it(`And an existing thread`, async () => {
            thread = (await ControllerApi.post({ content: "msg_0" }, serviceDiscovery._links.threads).expect(201)).body as Thread;
        });

        it(`With ${numberOfMessages} messages`, async () => {
            for (let i = 1; i < numberOfMessages; i++) {
                await ControllerApi.post({ content: `msg_${i}` }, thread._links.messages).expect(201);
            }
        });

        it("When I query the messages", async () => {
            response = await ControllerApi.get(thread._links.messages);
        });

        it("I verify valid status code", () => {
            response.statusCode.should.equal(200);
        });

        it("I verify the schema", () => {
            expect(response.body).be.validMessageQueryResultSchema();
            result = response.body as MessageQueryResult;
        });

        it("I verify that next link is not present", () => {
            expect(result._links.next).to.be.undefined;
        });

        it("I verify that prev link is present", () => {
            expect(result._links.prev).to.not.be.undefined;
        });

        it("I verify messages", () => {
            result._embedded.messages.should.have.lengthOf(limit);
            var orderedMessages = result._embedded.messages.sort(t => t.message_number);
            for (let i = 0; i < orderedMessages.length; i++) {
                orderedMessages[i].message_number.should.equal(numberOfMessages - limit + i + 1);
                orderedMessages[i].content.should.equal(`msg_${numberOfMessages - limit + i}`);
                orderedMessages[i].source.should.equal("user");
                orderedMessages[i].thread_id.should.equal(thread.thread_id);
                Date.parse(orderedMessages[i].created_on).should.be.greaterThanOrEqual(Date.parse(thread.created_on));
            }
        });

        it("When I request self link", async () => {
            response = await ControllerApi.get(result._links.self).expect(200);
        });

        it("Then I verify I get the same response", async () => {
            let resultSelf = response.body as MessageQueryResult;
            resultSelf.should.deep.equal(result);
        });

        it("When I add a new message to the thread", async () => {
            await ControllerApi.post({ content: `msg_${numberOfMessages}` }, thread._links.messages).expect(201);
        });

        it("When I request self link", async () => {
            response = await ControllerApi.get(result._links.self).expect(200);
        });

        it("Then I verify I got the same response apart from next link which should now be present", async () => {
            let resultSelf = structuredClone(response.body) as MessageQueryResult;
            expect(resultSelf._links.next).to.not.be.undefined;
            delete resultSelf._links.next;
            resultSelf.should.deep.equal(result);

            result = response.body as MessageQueryResult;
        });

        it("When I navigate to the next link", async () => {
            response = await ControllerApi.get(result._links.next!);
        });

        it("Then I verify correct status code", () => {
            response.statusCode.should.equal(200);
        });

        it("And I verify correct schema", () => {
            expect(response.body).to.have.validMessageQueryResultSchema();
        });

        it("And I verify links and messages", () => {
            let resultNext = response.body as MessageQueryResult;
            expect(resultNext._links.prev).to.not.be.undefined;
            expect(resultNext._links.next).to.be.undefined;

            expect(resultNext._links.last).to.deep.equal(result._links.last);
            expect(resultNext._links.first).to.deep.equal(result._links.first);

            resultNext._embedded.messages.should.have.lengthOf(1);
            resultNext._embedded.messages[0].message_number.should.equal(numberOfMessages + 1);
            resultNext._embedded.messages[0].content.should.equal(`msg_${numberOfMessages}`);
        });

        it("When I navigate to the prev link", async () => {
            response = await ControllerApi.get(result._links.prev!);
        });

        it("Then I verify correct status code", () => {
            response.statusCode.should.equal(200);
        });

        it("And I verify correct schema", () => {
            expect(response.body).to.have.validMessageQueryResultSchema();
        });

        it("And I verify links and messages", () => {
            let resultPrev = response.body as MessageQueryResult;
            expect(resultPrev._links.prev).to.not.be.undefined;
            expect(resultPrev._links.next).to.not.be.undefined;

            expect(resultPrev._links.last).to.deep.equal(result._links.last);
            expect(resultPrev._links.first).to.deep.equal(result._links.first);

            resultPrev._embedded.messages.should.have.lengthOf(limit);
            var orderedMessages = resultPrev._embedded.messages.sort(t => t.message_number);
            for (let i = 0; i < orderedMessages.length; i++) {
                orderedMessages[i].message_number.should.equal(numberOfMessages - 2 * limit + i + 1);
                orderedMessages[i].content.should.equal(`msg_${numberOfMessages - 2 * limit + i}`);
                orderedMessages[i].source.should.equal("user");
                orderedMessages[i].thread_id.should.equal(thread.thread_id);
                Date.parse(orderedMessages[i].created_on).should.be.greaterThanOrEqual(Date.parse(thread.created_on));
            }
        });

        it("When I navigate to the first link", async () => {
            response = await ControllerApi.get(result._links.first);
        });

        it("Then I verify correct status code", () => {
            response.statusCode.should.equal(200);
        });

        it("And I verify correct schema", () => {
            expect(response.body).to.have.validMessageQueryResultSchema();
        });

        it("And I verify links and messages", () => {
            let resultFirst = response.body as MessageQueryResult;
            expect(resultFirst._links.prev).to.be.undefined;
            expect(resultFirst._links.next).to.not.be.undefined;

            expect(resultFirst._links.last).to.deep.equal(result._links.last);
            expect(resultFirst._links.first).to.deep.equal(result._links.first);

            resultFirst._embedded.messages.should.have.lengthOf(limit);
            var orderedMessages = resultFirst._embedded.messages.sort(t => t.message_number);
            for (let i = 0; i < orderedMessages.length; i++) {
                orderedMessages[i].message_number.should.equal(i + 1);
                orderedMessages[i].content.should.equal(`msg_${i}`);
                orderedMessages[i].source.should.equal("user");
                orderedMessages[i].thread_id.should.equal(thread.thread_id);
                Date.parse(orderedMessages[i].created_on).should.be.greaterThanOrEqual(Date.parse(thread.created_on));
            }
        });

        it("When I navigate to the last link", async () => {
            response = await ControllerApi.get(result._links.last);
        });

        it("Then I verify correct status code", () => {
            response.statusCode.should.equal(200);
        });

        it("And I verify correct schema", () => {
            expect(response.body).to.have.validMessageQueryResultSchema();
        });

        it("And I verify links and messages", () => {
            let resultLast = response.body as MessageQueryResult;
            expect(resultLast._links.prev).to.not.be.undefined;
            expect(resultLast._links.next).to.be.undefined;

            expect(resultLast._links.last).to.deep.equal(result._links.last);
            expect(resultLast._links.first).to.deep.equal(result._links.first);

            resultLast._embedded.messages.should.have.lengthOf(limit);
            var orderedMessages = resultLast._embedded.messages.sort(t => t.message_number);
            for (let i = 0; i < orderedMessages.length; i++) {
                orderedMessages[i].message_number.should.equal(numberOfMessages + 1 - limit + i + 1);
                orderedMessages[i].content.should.equal(`msg_${numberOfMessages + 1 - limit + i}`);
                orderedMessages[i].source.should.equal("user");
                orderedMessages[i].thread_id.should.equal(thread.thread_id);
                Date.parse(orderedMessages[i].created_on).should.be.greaterThanOrEqual(Date.parse(thread.created_on));
            }
        });
    })
});