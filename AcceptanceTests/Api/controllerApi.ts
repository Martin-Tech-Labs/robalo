import request from 'supertest';
import Config from '../Config/config.ts';

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
};

export default ControllerApi;