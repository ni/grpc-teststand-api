# TestStand gRPC API TypeScript Example

## Running the Example

1. Ensure NPM is installed. See [the node installation documentation](https://docs.npmjs.com/downloading-and-installing-node-js-and-npm#using-a-node-installer-to-install-nodejs-and-npm) for details.
1. Save the files in this directory to a local directory.
1. From a command line in the local directory, run `npm install`. This will install NPM packages defined by the `package.json` file and the `package-lock.json` file.
1. Ensure the `server_config.json` for your TestStand gRPC server is set up to **TEMPORARILY** not require security and with CORS enabled:
   ```
   {
     "port": 5020,
     "security": {
       "server_cert": "",
       "server_key": "",
       "server_cert_pfx": "",
       "server_cert_pfx_password": "",
       "root_cert": ""
     },
     "cors": {
       "enable": true,
       "origins": [ "http://localhost:5173" ]
     }
   }
   ```
   and start running the server. **Remember to restore your security configuration when not using this example, and don't leave security disabled with production code!**

1. Run `npm run dev` from the command line. This starts a server on port 5173 hosting an HTML page that will execute TypeScript to call into the TestStand gRPC API.
1. From the command line, hitting the `o` key will launch a web browser to show the HTML. Clicking the button on the page will attempt to connect to the gRPC server and display the TestStand Engine major version number.
1. Hit the `q` key on the command line to stop running the server.

## Building the Example to Serve from an Existing Web Server

1. Follow the first three steps from `Running the Example` above.
1. From the command line in the local directory, run `npm run build`.
1. This creates a `dist` folder that contains an `index.html` file and an assets folder with dependencies including a bundled `.js` file. You can serve these files as static assets from an existing web server. See [this vite documentation](https://vitejs.dev/guide/static-deploy.html) for more information, including how to deploy to different types of servers.

## Creating a simple example from just the .proto files

1. Install NPM as described in step 1 of `Running the Example`.
1. From a command line at the parent folder where you want your project to go, follow the instructions [in the `vite` documentation](https://vitejs.dev/guide/#scaffolding-your-first-vite-project) to scaffold a project using `vite` and the `vanilla-ts` template.

   Note: This example is using `vite` as the application bundler for demonstration purposes. It is not required to use `vite` for your application.
1. Switch to your newly created project folder on the command line.
1. Run `npm install @protobuf-ts/plugin`.
1. Create a `protos` directory and copy the contents of the `content/protofiles` directory from [the NationalInstruments.TestStand.Grpc.Client Nuget package](https://www.nuget.org/packages/NationalInstruments.TestStand.Grpc.Client) to the new `protos` directory.
1. Switch to the `protos` directory and create a folder called `generated`.
1. Generate TypeScript files based on the .proto files by running:
   > npx protoc --ts_out generated --ts_opt ts_nocheck --ts_opt use_proto_field_name --ts_opt optimize_code_size --proto_path . .\ni\teststand\api\grpc\techpreview\common_types_api.proto .\ni\teststand\api\grpc\techpreview\NationalInstruments.TestStand.API.proto

   If you want to use files other than `TestStand.API`, either replace the filename with one that you want, or add more files to the end of the command (with spaces in between).

1. Replace the code in the `vite`-generated HTML and TypeScript files with the code you want to run.
1. See `src/main.ts` for a simple example of calling into the generated `NationalInstruments.TestStand.API.client.ts` file to use gRPC calls based on the corresponding .proto file.