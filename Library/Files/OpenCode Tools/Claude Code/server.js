import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { registerFixTypography } from "./tools/fix-typography.js";
import { registerPowershell } from "./tools/powershell.js";

const server = new McpServer( {
	name: "ewl",
	version: "1.0.0",
} );

registerFixTypography( server );
registerPowershell( server );

const transport = new StdioServerTransport();
await server.connect( transport );
