/// <reference types="node" />

import { styleText } from 'node:util';

import local from "./local.ts"
import qa from './qa.ts'

export interface Config {
    ControllerUrl: string;
}

export default getConfig();

export function getConfig(): Config {
    const environment = process.env.ENVIRONMENT ?? 'local';

    console.log(
        `${styleText('magenta', 'Using environment')} ${styleText(['bold', 'yellow'], environment.toUpperCase())}`
    );
    switch (environment) {
        case 'local': return local;
        case 'qa': return qa;
        default: throw new Error(`Unknown environment: ${environment}`);
    }
}