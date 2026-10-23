import {readdirSync,readFileSync,existsSync,statSync} from 'node:fs';
import {resolve,dirname,relative} from 'node:path';import {fileURLToPath} from 'node:url';
const root=resolve(dirname(fileURLToPath(import.meta.url)),'..'),files=[],failures=[];
const excluded=new Set(['.git','node_modules','bin','obj','.superpowers','.angular','dist','test-results','backups','.codex','.agents']);
function scan(dir){for(const entry of readdirSync(dir,{withFileTypes:true})){if(entry.isDirectory()){if(!excluded.has(entry.name))scan(resolve(dir,entry.name));}else if(entry.name.endsWith('.md'))files.push(resolve(dir,entry.name));}}
function prose(text){let fenced=false;return text.split(/\r?\n/).filter(line=>{if(/^\s*(```|~~~)/.test(line)){fenced=!fenced;return false;}return !fenced;}).join('\n');}
const slugs=new Map();
function anchors(path){if(slugs.has(path))return slugs.get(path);const text=prose(readFileSync(path,'utf8')),ids=new Set(),counts=new Map();for(const line of text.split('\n')){const heading=/^\s{0,3}#{1,6}\s+(.+?)\s*#*\s*$/.exec(line);if(heading){const base=heading[1].replace(/<[^>]+>/g,'').replace(/[`*_~]/g,'').toLowerCase().replace(/[^\p{L}\p{N}\p{M}_\-\s]/gu,'').replace(/\s/g,'-');const count=counts.get(base)??0;counts.set(base,count+1);ids.add(base+(count?'-'+count:''));}}for(const match of text.matchAll(/\b(?:id|name)=["']([^"']+)["']/g))ids.add(match[1]);slugs.set(path,ids);return ids;}
scan(root);
for(const file of files){const text=prose(readFileSync(file,'utf8')).replace(/`[^`\n]+`/g,'');for(const match of text.matchAll(/!?\[[^\]]*\]\((<[^>]+>|[^\s)]+)(?:\s+["'][^"']*["'])?\)/g)){
 const url=match[1].replace(/^<|>$/g,'');if(/^[a-z][a-z\d+.-]*:/i.test(url)||url.startsWith('//'))continue;
 let decoded;try{decoded=decodeURIComponent(url);}catch{failures.push(relative(root,file)+': invalid URL '+url);continue;}
 const [rawPath,anchor]=decoded.split('#'),target=rawPath?resolve(dirname(file),rawPath.split('?')[0]):file;
 if(!existsSync(target)){failures.push(relative(root,file)+': missing '+url);continue;}
 if(anchor&&target.endsWith('.md')&&statSync(target).isFile()&&!anchors(target).has(anchor))failures.push(relative(root,file)+': missing anchor '+url);
}}
if(failures.length){console.error(failures.join('\n'));process.exitCode=1;}else console.log('PASS: internal Markdown links and heading anchors in '+files.length+' files.');
