#!/usr/bin/env node
/**
 * Build-time wiki ingestion: reads markdown from .wiki-src/ and emits wiki.ts.
 * Run via npm prebuild/predev; wiki source is copied from docs/wiki/ by `make webinterface`.
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

/**
 * Wiki sidebar order + groups — mirrors ModConfigSectionGroups.PreferredSectionOrder
 * (settings-mirrored IDs). Client extras (custom-assets, web-dashboard) are wiki-only.
 * Overview (README) is injected separately as the General slot in Client.
 */
const WIKI_NAV_ORDER = [
  // Client (after Overview): Privacy, User Interface, then wiki-only extras
  { id: 'privacy', groupId: 'client' },
  { id: 'user-interface', groupId: 'client' },
  { id: 'custom-assets', groupId: 'client' },
  { id: 'web-dashboard', groupId: 'client' },

  // Session: Prep first, then A–Z
  { id: 'savegame-preparation', groupId: 'session' },
  { id: 'join-anytime', groupId: 'session' },
  { id: 'more-players', groupId: 'session' },
  { id: 'more-voices', groupId: 'session' },
  { id: 'persistence', groupId: 'session' },
  { id: 'player-announcements', groupId: 'session' },
  { id: 'statistics', groupId: 'session' },

  // Balance: A–Z
  { id: 'dungeon-time', groupId: 'balance' },
  { id: 'economy', groupId: 'balance' },
  { id: 'loot-multiplicator', groupId: 'balance' },
  { id: 'spawn-scaling', groupId: 'balance' },

  // World: A–Z
  { id: 'dungeon-randomizer', groupId: 'world' },
  { id: 'mimic-tuning', groupId: 'world' },
  { id: 'player-tuning', groupId: 'world' },
  { id: 'weather', groupId: 'world' },
];

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

/**
 * Badge metadata from markdown. Preferred form:
 *   **Scope:** host
 *   **Scope:** local
 *   **Scope:** host,local
 * Optional trailing " · **Config:** ..." is ignored for badges.
 * Falls back to a leading **Host only** / **Local only** line when **Scope:** is absent.
 */
function extractScopes(markdown) {
  const scopes = [];
  const push = (token) => {
    if (token === 'host' || token === 'local') {
      if (!scopes.includes(token)) scopes.push(token);
    }
  };

  const parseScopeValue = (raw) => {
    const text = raw.toLowerCase();
    const parts = text.split(/\s*,\s*/);
    let matchedPart = false;
    for (const part of parts) {
      const p = part.trim();
      if (!p) continue;
      if (p === 'local' || /\blocal only\b/.test(p) || /\byour game only\b/.test(p)) {
        push('local');
        matchedPart = true;
        continue;
      }
      if (p === 'host' || /\bhost only\b/.test(p)) {
        push('host');
        matchedPart = true;
        continue;
      }
    }
    if (!matchedPart) {
      if (/\blocal\b/.test(text) || /\byour game\b/.test(text)) push('local');
      if (/\bhost\b/.test(text)) push('host');
    }
  };

  const scopeLine = markdown.match(/^\*\*Scope:\*\*\s*([^\n]+)/m);
  if (scopeLine) {
    // Drop trailing "· **Config:** ..." (and similar) so only the badge value is parsed.
    const value = scopeLine[1].split(/\s*·\s*\*\*/)[0].trim();
    parseScopeValue(value);
    return scopes;
  }

  // Fallback: first **Host only** / **Local only** lead-in near the top of the article.
  const head = markdown.slice(0, 800);
  if (/\*\*Host only\*\*/i.test(head)) push('host');
  if (/\*\*Local only\*\*/i.test(head)) push('local');
  return scopes;
}

/** Remove badge metadata from body HTML; keep trailing **Config:** when present. */
function stripScopeMetadata(markdown) {
  return markdown.replace(
    /^\*\*Scope:\*\*\s*([^\n]*)$/m,
    (_, rest) => {
      const config = rest.match(/(?:\s*·\s*)?(\*\*Config:\*\*[^\n]*)/);
      return config ? config[1].trim() : '';
    },
  ).replace(/\n{3,}/g, '\n\n');
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

function processArticle(id, markdown, groupId = '') {
  const title = extractTitle(markdown);
  const subSections = extractSubSections(markdown);
  const scopes = extractScopes(markdown);
  let html = marked.parse(stripScopeMetadata(markdown));
  html = rewriteLinks(html);
  html = addHeadingIds(html, subSections);
  return { id, title, html, subSections, scopes, groupId };
}

function main() {
  if (!fs.existsSync(WIKI_ROOT)) {
    console.error(`error: wiki source not found at ${WIKI_ROOT}`);
    console.error('Run `make webinterface` or copy docs/wiki to .wiki-src');
    process.exit(1);
  }

  const readmePath = path.join(WIKI_ROOT, 'README.md');
  if (!fs.existsSync(readmePath)) {
    console.error('error: missing .wiki-src/README.md');
    process.exit(1);
  }

  const readmeContent = fs.readFileSync(readmePath, 'utf8');
  const overview = processArticle('overview', readmeContent, 'client');

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
  for (const { id, groupId } of WIKI_NAV_ORDER) {
    if (byId.has(id)) {
      const article = byId.get(id);
      article.groupId = groupId;
      ordered.push(article);
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
  groupId: string;
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
