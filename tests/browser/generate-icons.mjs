// Reproducible raster exports of the existing SVG, using the browser renderer.
// Run in the browser-test image with public/ mounted at /icons.
import { chromium } from '@playwright/test';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
const svg = await readFile('/icons/plantcare-icon.svg', 'utf8');
const browser = await chromium.launch();
try {
  const page = await browser.newPage();
  await mkdir('/icons/icons', { recursive: true });
  for (const size of [180, 192, 512]) {
    const data = await page.evaluate(async ({ svg, size }) => {
      const image = new Image();
      image.src = 'data:image/svg+xml;base64,' + btoa(svg);
      await image.decode();
      const canvas = document.createElement('canvas');
      canvas.width = canvas.height = size;
      const context = canvas.getContext('2d');
      context.fillStyle = '#245c36'; context.fillRect(0, 0, size, size);
      context.drawImage(image, 0, 0, size, size);
      return canvas.toDataURL('image/png').split(',')[1];
    }, { svg, size });
    await writeFile(`/icons/icons/plantcare-${size}.png`, Buffer.from(data, 'base64'));
  }
} finally { await browser.close(); }
