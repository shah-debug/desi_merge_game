// Desi Merge — Google Play screenshots with goldie's framing step.
// Raw captures come from a real Pixel 7 Pro (adb screencap), listed in out/raw/pixel-10-pro/manifest.json,
// so `goldie frame` / `goldie verify` run without argent or an emulator. Flows are not used.
const en = (s: string) => ({ "en-US": s });

export default {
  appRoot: ".",
  flowsDir: "./flows",
  appPath: "",
  bundleId: "com.DefaultCompany.DesiMerge",
  android: {
    appPath: "C:/Users/HP/DesiMerge/Builds/Android/DesiMerge-rc-unsigned-test.apk",
    applicationId: "com.DefaultCompany.DesiMerge",
  },
  devices: ["pixel-10-pro"],
  locales: ["en-US"],
  appearance: "light",
  frame: { variant: "17-pro-orange" },
  theme: {
    // The game's own palette: cream background warming to saffron, navy headlines, amber subheads.
    background: "linear-gradient(170deg, #FFF4DE 0%, #FFE2A8 55%, #FFC870 100%)",
    headlineColor: "#1B1F4B",
    subheadColor: "#B4590A",
    fontFamily: '"Desi Merge", "Lato", sans-serif',
    fontFiles: [
      { family: "Desi Merge", files: { 400: "./LilitaOne-Regular.ttf" } },
    ],
    copyHeightRatio: 0.24,
    deviceWidthRatio: 0.82,
    layout: "classic",
    template: "uniform",
  },
  store: {
    name: "Desi Merge",
    subtitle: en("Milao • Barhao • Jeeto"),
    developer: "Desi Merge",
    category: "Puzzle",
    rating: 0,
    ratingCount: "New",
    ageRating: "Everyone",
    price: "Free",
    description: en("A Pakistani-themed merge puzzle: drop, match and merge desi favourites from patti to rickshaw."),
  },
  scenes: [
    { kind: "screenshot", id: "play", flow: "unused-play", headline: en("Drop, match & merge"), subhead: en("Desi favourites from patti to rickshaw") },
    { kind: "screenshot", id: "menu", flow: "unused-menu", headline: en("Milao • Barhao • Jeeto"), subhead: en("A Pakistani merge puzzle") },
    { kind: "screenshot", id: "hammer", flow: "unused-hammer", headline: en("Smash with the Hammer"), subhead: en("Clear space when the jar gets tight") },
    { kind: "screenshot", id: "collection", flow: "unused-collection", headline: en("Collect 50 desi items"), subhead: en("Every merge fills your collection") },
    { kind: "screenshot", id: "skins", flow: "unused-skins", headline: en("Unlock new skins"), subhead: en("Mithai, Street Food, Truck Art & more") },
    { kind: "screenshot", id: "missions", flow: "unused-missions", headline: en("Daily missions"), subhead: en("Fresh challenges every day and week") },
    { kind: "screenshot", id: "daily", flow: "unused-daily", headline: en("Rewards every day"), subhead: en("Diamonds and power-ups for your streak") },
  ],
};
