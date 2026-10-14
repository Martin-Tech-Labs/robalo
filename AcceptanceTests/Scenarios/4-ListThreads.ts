import 'chai/register-should.js';
import "../Api/SchemaAssertions.ts";

import { describe, it } from 'mocha';
import { expect } from "chai";

import ControllerApi from '../Api/controllerApi.ts'
import type { Response } from 'supertest';

import type { ServiceDiscovery } from '../Api/Types/service-discovery.d.ts'
import type { Thread } from '../Api/Types/thread.d.ts';
import type { MessageQueryResult } from '../Api/Types/message-query-result.d.ts'

import type { ThreadList } from '../Api/Types/thread-list.d.ts'

import { randomUUID } from 'node:crypto';

describe('4 - List Threads @listThreads @threads', () => {
    let serviceDiscovery: ServiceDiscovery;
    let thread1: Thread;
    let thread2: Thread;
    let response: Response;
    let message: string;
    let threadList: ThreadList;

    describe('List Threads', () => {
        before(() => {
            message = `msg_${randomUUID().toLowerCase()}`;
        });

        it("Given service discovery document", async () => {
            serviceDiscovery = (await ControllerApi.serviceDiscovery()).body as ServiceDiscovery;
        });


        it(`And an existing thread 1`, async () => {
            thread1 = (await ControllerApi.post({ content: message }, serviceDiscovery._links['create-thread'])).body as Thread;
        });

        it(`And an existing thread 2`, async () => {
            thread2 = (await ControllerApi.post({ content: message }, serviceDiscovery._links['create-thread'])).body as Thread;
        });

        it("When I request a list of threads", async () => {
            response = await ControllerApi.get(serviceDiscovery._links.threads);
        });

        it("Then I verify correct status code", async () => {
            response.statusCode.should.equal(200);
        });

        it("And I verfy the content type application/hal+json", () => {
            response.headers['content-type'].should.include('application/hal+json');
        });

        it("And I verfy the schema", () => {
            expect(response.body).to.have.validThreadListSchema();
            threadList = response.body as ThreadList;
        });

        it("When I go to the self link", async () => {
            response = await ControllerApi.get(threadList._links.self);
        });

        it("Then I verify correct status code", async () => {
            response.statusCode.should.equal(200);
        });

        it("And I verfy the content type application/hal+json", () => {
            response.headers['content-type'].should.include('application/hal+json');
        });

        it("And I verfy the schema", () => {
            expect(response.body).to.have.validThreadListSchema();
        });

        it("And I verfy the result is identical to the previous one", () => {
            let threadListSelf = response.body as ThreadList;
            threadList.should.deep.equal(threadListSelf);
        });

        it("And I verfy the thread 1 is present in the result", () => {
            let threads = threadList._embedded.threads.filter(t => t.thread_id == thread1.thread_id);
            expect(threads).to.have.lengthOf(1);
            threads[0].should.deep.equal(thread1);
        });

        it("And I verfy the thread 2 is present in the result", () => {
            let threads = threadList._embedded.threads.filter(t => t.thread_id == thread2.thread_id);
            expect(threads).to.have.lengthOf(1);
            threads[0].should.deep.equal(thread2);
        });

        it("When I delete the thread 1", async () => {
            await ControllerApi.delete(thread1._links.self).expect(204);
        });

        it("When I get again the threads list", async () => {
            response = await ControllerApi.get(threadList._links.self).expect(200);
            threadList = response.body as ThreadList;
        });

        it("And I verfy the thread 1 is not present in the result", () => {
            let threads = threadList._embedded.threads.filter(t => t.thread_id == thread1.thread_id);
            expect(threads).to.have.lengthOf(0);
        });

        it("And I verfy the thread 2 is present in the result", () => {
            let threads = threadList._embedded.threads.filter(t => t.thread_id == thread2.thread_id);
            expect(threads).to.have.lengthOf(1);
            threads[0].should.deep.equal(thread2);
        });
    })
});