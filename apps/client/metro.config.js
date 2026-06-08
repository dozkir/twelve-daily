const { getDefaultConfig } = require("expo/metro-config");
const path = require("path");

const projectRoot = __dirname;
const workspaceRoot = path.resolve(projectRoot, "../..");

const config = getDefaultConfig(projectRoot);

// Watch the monorepo root
config.watchFolders = [workspaceRoot];

// Let Metro resolve packages from the workspace root node_modules too
config.resolver.nodeModulesPaths = [
  path.resolve(projectRoot, "node_modules"),
  path.resolve(workspaceRoot, "node_modules")
];

// Runtime path alias for @/
config.resolver.alias = {
  "@": projectRoot
};

module.exports = config;
