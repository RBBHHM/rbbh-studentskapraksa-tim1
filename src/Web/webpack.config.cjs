const path = require("node:path");
const CopyPlugin = require("copy-webpack-plugin");
const HtmlWebpackPlugin = require("html-webpack-plugin");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");

module.exports = (_env, argv) => {
  const production = argv.mode === "production";
  return {
    mode: production ? "production" : "development",
    entry: "./src/client.tsx",
    output: {
      path: path.resolve(__dirname, "dist"),
      filename: production ? "assets/app.[contenthash].js" : "assets/app.js",
      chunkFilename: production ? "assets/chunk.[contenthash].js" : "assets/chunk.[name].js",
      clean: true,
      publicPath: "/",
    },
    optimization: production
      ? {
          runtimeChunk: "single",
          splitChunks: {
            chunks: "all",
            cacheGroups: {
              vendor: {
                test: /[\\/]node_modules[\\/]/,
                name: "vendor",
                priority: 10,
                reuseExistingChunk: true,
              },
            },
          },
        }
      : undefined,
    devtool: production ? "source-map" : "eval-cheap-module-source-map",
    resolve: {
      extensions: [".tsx", ".ts", ".jsx", ".js"],
      alias: { "@": path.resolve(__dirname, "src") },
    },
    module: {
      rules: [
        { test: /\.tsx?$/, exclude: /node_modules/, use: { loader: "ts-loader", options: { transpileOnly: true, compilerOptions: { noEmit: false, allowImportingTsExtensions: false } } } },
        {
          test: /\.css$/,
          use: [production ? MiniCssExtractPlugin.loader : "style-loader", { loader: "css-loader", options: { url: false } }, "postcss-loader"],
        },
      ],
    },
    plugins: [
      new HtmlWebpackPlugin({ template: "./public/index.html" }),
      new CopyPlugin({
        patterns: [
          { from: "public", to: ".", globOptions: { ignore: ["**/index.html"] } },
          ...(!production ? [{ from: ".runtime/app-config.js", to: "app-config.js" }] : []),
        ],
      }),
      ...(production ? [new MiniCssExtractPlugin({ filename: "assets/app.[contenthash].css" })] : []),
    ],
    devServer: {
      host: "127.0.0.1",
      port: 8080,
      historyApiFallback: true,
      hot: true,
      proxy: [
        { context: ["/api", "/health", "/swagger"], target: "http://127.0.0.1:5000" },
      ],
    },
  };
};
