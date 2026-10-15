/**
 * Upload frontend/dist to Hostinger via SFTP (SSH).
 * Credentials via env only — never commit passwords.
 *
 *   set HOSTINGER_SSH_HOST=82.25.100.95
 *   set HOSTINGER_SSH_PORT=65002
 *   set HOSTINGER_SSH_USER=u910121167_...
 *   set HOSTINGER_SSH_PASSWORD=...
 *   node scripts/deploy-hostinger-sftp.mjs
 */
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import SftpClient from 'ssh2-sftp-client'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const repoRoot = path.resolve(__dirname, '..')
const localDist = path.join(repoRoot, 'frontend', 'dist')

const host = process.env.HOSTINGER_SSH_HOST
const port = Number(process.env.HOSTINGER_SSH_PORT || '22')
const username = process.env.HOSTINGER_SSH_USER
const password = process.env.HOSTINGER_SSH_PASSWORD
let remoteDir = process.env.HOSTINGER_SSH_REMOTE_DIR?.trim()

if (!host || !username || !password) {
  console.error('Set HOSTINGER_SSH_HOST, HOSTINGER_SSH_USER, HOSTINGER_SSH_PASSWORD (and optional PORT / REMOTE_DIR).')
  process.exit(1)
}

if (!fs.existsSync(localDist)) {
  console.error('Missing frontend/dist — run npm run build in frontend/ first.')
  process.exit(1)
}

async function uploadDir(sftp, local, remote) {
  await sftp.mkdir(remote, true)
  for (const name of fs.readdirSync(local)) {
    const localPath = path.join(local, name)
    const remotePath = `${remote}/${name}`
    const stat = fs.statSync(localPath)
    if (stat.isDirectory()) {
      await uploadDir(sftp, localPath, remotePath)
    } else {
      await sftp.put(localPath, remotePath)
      console.log(`uploaded ${remotePath}`)
    }
  }
}

async function detectPublicHtml(sftp) {
  const candidates = [
    remoteDir,
    '/home/u910121167/websites/cVDFfe0wZ/public_html',
    '/domains/dimgrey-hummingbird-677957.hostingersite.com/public_html',
    'public_html',
  ].filter(Boolean)

  for (const candidate of candidates) {
    try {
      const listing = await sftp.list(candidate)
      if (listing?.length >= 0) {
        return candidate.replace(/\/$/, '')
      }
    } catch {
      // try next
    }
  }

  const home = await sftp.realPath('.')
  const guess = `${home}/domains/dimgrey-hummingbird-677957.hostingersite.com/public_html`
  try {
    await sftp.list(guess)
    return guess
  } catch {
    throw new Error(`Could not find public_html. Set HOSTINGER_SSH_REMOTE_DIR. Home: ${home}`)
  }
}

const sftp = new SftpClient()
try {
  await sftp.connect({ host, port, username, password, readyTimeout: 30000 })
  const target = await detectPublicHtml(sftp)
  console.log(`Deploying to ${target}`)
  await uploadDir(sftp, localDist, target)
  console.log('Deploy complete.')
} finally {
  await sftp.end()
}
