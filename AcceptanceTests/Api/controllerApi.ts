import request from 'supertest';
import Config from '../Config/config.ts';

import type { MessageRequest } from './Types/message-request.d.ts'
import type { Link } from './Types/link.d.ts';
import { createEventSource } from "eventsource-client";


const ControllerApi = {
    ping() {
        return request(Config.ControllerUrl).get('/_system/ping');
    },

    metrics() {
        return request(Config.ControllerUrl).get('/_system/metrics');
    },

    health() {
        return request(Config.ControllerUrl).get('/_system/health');
    },
    env() {
        return request(Config.ControllerUrl).get('/_system/env');
    },
    serviceDiscovery: () => request(Config.ControllerUrl).get(''),

    get: (link: Link) => request(link.href).get(''),

    subscribe: async (link: Link) => createEventSource(link.href),

    delete: (link: Link) => request(link.href).delete(''),

    post: <T extends object | string>(content: T, link: Link) =>
        request(link.href)
            .post('')
            .set("Content-Type", "application/json")
            .send(content)
};

export default ControllerApi;