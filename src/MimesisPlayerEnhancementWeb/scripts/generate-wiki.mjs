#!/usr/bin/env node
/**
 * Build-time wiki ingestion: reads markdown from .wiki-src/ and emits wiki.ts.
 * Run via npm prebuild/predev; wiki source is copied from docs/wiki/ by build-webdashboard.sh.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { marked } from 'marked';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const WEB_ROOT = path.resolve(__dirname, '..');
const WIKI_ROOT = path.join(WEB_ROOT, '.wiki-src');
const OUT_DIR = path.join(WEB_ROOT, 'src/lib/generated');
const OUT_FILE = path.join(OUT_DIR, 'wiki.ts');

const GITHUB_REPO =
  'https://github.com/Kandru/mimesis-player-enhancements/blob/main';

marked.setOptions({ gfm: true, breaks: false });

function slugify(text) {
  return text
    .toLowerCase()
    .replace(/[^\w\s-]/g, '')
    .trim()
    .replace(/\s+/g, '-');
}

function extractTitle(markdown) {
  const match = markdown.match(/^#\s+(.+)$/m);
  return match ? match[1].trim() : 'Untitled';
}

function extractSubSections(markdown) {
  const sections = [];
  for (const line of markdown.split('\n')) {
    const match = line.match(/^##\s+(.+)$/);
    if (match) {
      const title = match[1].trim();
      sections.push({ id: slugify(title), title });
    }
  }
  return sections;
}

function extractScopes(markdown) {
  const match = markdown.match(/\*\*Scope:\*\*\s*([^\n]+)/);
  if (!match) return [];
  const raw = match[1].trim().toLowerCase();
  const scopes = [];
  if (raw.includes('host process') || raw.includes('host only') || raw.includes('host')) scopes.push('host');
  if (raw.includes('local') || raw.includes('your game')) scopes.push('local');
  return [...new Set(scopes)];
}

function parseNavOrder(readmeContent) {
  const order = [];
  const re = /\]\((?:\.\/)?features\/([a-z0-9-]+)\.md\)/g;
  let m;
  while ((m = re.exec(readmeContent)) !== null) {
    if (!order.includes(m[1])) order.push(m[1]);
  }
  return order;
}

function rewriteLinks(html) {
  let out = html;

  // Internal wiki feature links → dashboard hash routes
  out = out.replace(
    /href="(?:\.\/)?features\/([a-z0-9-]+)\.md(?:#[^"]*)?"/g,
    'href="#/home/$1"',
  );

  // Overview / README links
  out = out.replace(/href="\.\/README\.md(?:#[^"]*)?"/g, 'href="#/home"');
  out = out.replace(/href="README\.md(?:#[^"]*)?"/g, 'href="#/home"');

  // Relative sibling feature links (from within features/)
  out = out.replace(
    /href="([a-z0-9-]+)\.md(?:#[^"]*)?"/g,
    'href="#/home/$1"',
  );

  // CONFIG.md and other docs → GitHub blob URLs
  out = out.replace(
    /href="\.\.\/([^"]+\.md)(#[^"]*)?"/g,
    (_, file, anchor) => `href="${GITHUB_REPO}/docs/${file}${anchor ?? ''}" target="_blank" rel="noopener noreferrer"`,
  );

  // LOOT_ITEM_IDS etc. already under docs/
  out = out.replace(
    /href="(\.\.\/)?([A-Z_]+\.md)(#[^"]*)?"/g,
    (_, _prefix, file, anchor) =>
      `href="${GITHUB_REPO}/docs/${file}${anchor ?? ''}" target="_blank" rel="noopener noreferrer"`,
  );

  return out;
}

function addHeadingIds(html, subSections) {
  let idx = 0;
  return html.replace(/<h2>([^<]+)<\/h2>/g, (_, title) => {
    const trimmed = title.trim();
    const section = subSections[idx];
    idx += 1;
    const id = section?.title === trimmed ? section.id : slugify(trimmed);
    return `<h2 id="${id}">${title}</h2>`;
  });
}

function processArticle(id, markdown) {
  const title = extractTitle(markdown);
  const subSections = extractSubSections(markdown);
  const scopes = extractScopes(markdown);
  let html = marked.parse(markdown);
  html = rewriteLinks(html);
  html = addHeadingIds(html, subSections);
  return { id, title, html, subSections, scopes };
}

function main() {
  if (!fs.existsSync(WIKI_ROOT)) {
    console.error(`error: wiki source not found at ${WIKI_ROOT}`);
    console.error('Run build-webdashboard.sh or copy docs/wiki to .wiki-src');
    process.exit(1);
  }

  const readmePath = path.join(WIKI_ROOT, 'README.md');
  if (!fs.existsSync(readmePath)) {
    console.error('error: missing .wiki-src/README.md');
    process.exit(1);
  }

  const readmeContent = fs.readFileSync(readmePath, 'utf8');
  const navOrder = parseNavOrder(readmeContent);
  const overview = processArticle('overview', readmeContent);

  const featuresDir = path.join(WIKI_ROOT, 'features');
  const featureFiles = fs.existsSync(featuresDir)
    ? fs.readdirSync(featuresDir).filter((f) => f.endsWith('.md'))
    : [];

  const byId = new Map();
  for (const file of featureFiles) {
    const id = file.replace(/\.md$/, '');
    const content = fs.readFileSync(path.join(featuresDir, file), 'utf8');
    byId.set(id, processArticle(id, content));
  }

  const ordered = [];
  for (const id of navOrder) {
    if (byId.has(id)) {
      ordered.push(byId.get(id));
      byId.delete(id);
    }
  }
  const remaining = [...byId.keys()].sort();
  for (const id of remaining) {
    ordered.push(byId.get(id));
  }

  const wikiById = { overview, ...Object.fromEntries(ordered.map((a) => [a.id, a])) };

  fs.mkdirSync(OUT_DIR, { recursive: true });

  const ts = `/* eslint-disable */
// Auto-generated by scripts/generate-wiki.mjs — do not edit.

export interface WikiSubSection {
  id: string;
  title: string;
}

export interface WikiArticle {
  id: string;
  title: string;
  html: string;
  subSections: WikiSubSection[];
  scopes: ('host' | 'local')[];
}

export const wikiOverview: WikiArticle = ${JSON.stringify(overview, null, 2)};

export const wikiArticles: WikiArticle[] = ${JSON.stringify(ordered, null, 2)};

export const wikiById: Record<string, WikiArticle> = ${JSON.stringify(wikiById, null, 2)};
`;

  fs.writeFileSync(OUT_FILE, ts, 'utf8');
  console.log(
    `Generated wiki.ts — overview + ${ordered.length} feature articles → ${OUT_FILE}`,
  );
}

main();
