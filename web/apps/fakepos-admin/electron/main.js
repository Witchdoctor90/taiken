const { app, BrowserWindow } = require("electron");
const path = require("node:path");

function createWindow() {
  const window = new BrowserWindow({
    width: 1000,
    height: 700,
    title: "Fake POS — термінал",
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
    },
  });

  window.loadFile(path.join(__dirname, "..", "dist", "index.html"));
}

app.whenReady().then(createWindow);

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});
