import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

const isProduction = env.NODE_ENV === 'production';
const inDocker = fs.existsSync('/.dockerenv') || fs.existsSync('/.dockerinit') || env.IN_DOCKER === 'true';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = "bonlinehomeassignement.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

// Only attempt to create or load dev HTTPS certificates in local non-production
// scenarios where the dotnet dev-certs tool is available. Inside Docker (or in
// production builds) dotnet may not be present and certificate creation will fail.
if (!isProduction && !inDocker) {
    if (!fs.existsSync(baseFolder)) {
        fs.mkdirSync(baseFolder, { recursive: true });
    }

    if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
        if (0 !== child_process.spawnSync('dotnet', [
            'dev-certs',
            'https',
            '--export-path',
            certFilePath,
            '--format',
            'Pem',
            '--no-password',
        ], { stdio: 'inherit', }).status) {
            throw new Error("Could not create certificate.");
        }
    }
}

// In Docker, use the internal container hostname; otherwise use localhost
const backendHost = inDocker ? 'bonlinehomeassignement-server' : 'localhost';
const target = env.ASPNETCORE_HTTPS_PORT ? `https://${backendHost}:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : `http://${backendHost}:5000`;

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            '^/api': {
                target,
                secure: false
            }
        },
        // Bind to 0.0.0.0 so the dev server is reachable from the host via
        // Docker port mappings. Default port is 3000 to match common tooling
        // and the docker-compose.override mapping.
        host: '0.0.0.0',
        port: parseInt(env.DEV_SERVER_PORT || '3000'),
        // Only enable HTTPS for the dev server when not in production build or Docker.
        ...((isProduction || inDocker) ? {} : {
            https: {
                key: fs.readFileSync(keyFilePath),
                cert: fs.readFileSync(certFilePath),
            }
        })
    }
})
