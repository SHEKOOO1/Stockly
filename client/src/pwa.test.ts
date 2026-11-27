import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

interface ManifestIcon {
  src: string;
  sizes: string;
  type: string;
  purpose?: string;
}

interface WebManifest {
  id?: string;
  name: string;
  short_name: string;
  description: string;
  lang: string;
  dir: string;
  start_url: string;
  scope: string;
  display: string;
  background_color: string;
  theme_color: string;
  icons: ManifestIcon[];
  prefer_related_applications?: boolean;
}

function readPublicText(fileName: string) {
  return readFileSync(resolve(process.cwd(), 'public', fileName), 'utf8');
}

function publicFileExists(src: string) {
  return existsSync(resolve(process.cwd(), 'public', src.replace(/^\//, '')));
}

const manifest = JSON.parse(readPublicText('manifest.webmanifest')) as WebManifest;
const indexHtml = readFileSync(resolve(process.cwd(), 'index.html'), 'utf8');

describe('PWA installability configuration', () => {
  it('declares stable Arabic standalone app identity', () => {
    expect(manifest.name).toBe('ستوكلي — منصة إدارة المخازن');
    expect(manifest.short_name).toBe('ستوكلي');
    expect(manifest.lang).toBe('ar');
    expect(manifest.dir).toBe('rtl');
    expect(manifest.display).toBe('standalone');
    expect(manifest.id).toBe('/');
    expect(manifest.start_url).toBe('/');
    expect(manifest.scope).toBe('/');
    expect(manifest.prefer_related_applications).not.toBe(true);
  });

  it('declares installable icons that all exist', () => {
    expect(
      manifest.icons.some((icon) => icon.sizes === '192x192' && icon.type === 'image/png'),
    ).toBe(true);
    expect(
      manifest.icons.some((icon) => icon.sizes === '512x512' && icon.type === 'image/png'),
    ).toBe(true);
    expect(
      manifest.icons.some(
        (icon) =>
          icon.src === '/icon.svg' && icon.type === 'image/svg+xml' && icon.purpose === 'any',
      ),
    ).toBe(true);
    expect(
      manifest.icons.some(
        (icon) =>
          icon.src === '/icon-maskable.svg' &&
          icon.type === 'image/svg+xml' &&
          icon.purpose === 'maskable',
      ),
    ).toBe(true);
    for (const icon of manifest.icons) {
      expect(publicFileExists(icon.src)).toBe(true);
    }
  });

  it('does not claim offline writes', () => {
    expect(manifest.description).toContain('لا كتابة دون اتصال');
  });

  it('keeps HTML RTL metadata aligned with the manifest', () => {
    expect(indexHtml).toContain('<html lang="ar" dir="rtl">');
    expect(indexHtml).toContain('name="viewport" content="width=device-width, initial-scale=1.0"');
    expect(indexHtml).toContain('rel="manifest" href="/manifest.webmanifest"');
    expect(indexHtml).toContain('rel="apple-touch-icon" href="/icon-192.png"');
    const themeColor = /<meta name="theme-color" content="([^"]+)"/.exec(indexHtml)?.[1];
    expect(themeColor).toBe(manifest.theme_color);
  });
});
