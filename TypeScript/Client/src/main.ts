import '@ni/nimble-components/dist/esm/button';
import type { Button } from '@ni/nimble-components/dist/esm/button';
import { Guid } from "guid-typescript";

import {
    GrpcWebFetchTransport,
    GrpcWebOptions
} from '@protobuf-ts/grpcweb-transport';
import { EngineClient } from '../protos/generated/ni/teststand/api/grpc/techpreview/NationalInstruments.TestStand.API.client';

async function getEngineVersion() {
    const options: GrpcWebOptions = {
        baseUrl: 'http://localhost:5020',
        meta: { 'connection-id': Guid.raw() }
    };

    const transport = new GrpcWebFetchTransport(options);
    const client = new EngineClient(transport);

    const { response } = await client.engine({});
    const engineInstance = response.returnValue;

    const versionResponse = await client.getMajorVersion({
        instance: engineInstance
    });
    const majorVersion = versionResponse.response.returnValue;
    return majorVersion;
}

setupButton(
    document.querySelector<Button>('#counter')!,
    document.querySelector('#version')!
);

function setupButton(button: Button, text: Element) {
    button.addEventListener('click', async () => {
        button.disabled = true;
        try {
            text.textContent = 'Running';
            const majorVersion = await getEngineVersion();
            text.textContent = `Major version is ${majorVersion}`;
        } catch (err: unknown) {
            if (err instanceof Error) {
                text.textContent = `Error: ${err.message}`;
            } else {
                text.textContent = 'Error';
            }
        } finally {
            button.disabled = false;
        }
    });
}
