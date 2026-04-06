import type { VercelRequest, VercelResponse } from '@vercel/node';

export function ensureGetOrHead(req: VercelRequest, res: VercelResponse): boolean {
  if (req.method !== 'GET' && req.method !== 'HEAD') {
    res.setHeader('Allow', 'GET, HEAD');
    res.status(405).end();
    return false;
  }
  return true;
}

export async function proxyUpstream(
  req: VercelRequest,
  res: VercelResponse,
  options: {
    target: string;
    headers: Record<string, string>;
    timeoutMs?: number;
    logTag: string;
    upstreamErrorMessage: string;
  },
): Promise<void> {
  const { target, headers, timeoutMs = 25_000, logTag, upstreamErrorMessage } = options;
  try {
    const upstream = await fetch(target, {
      method: req.method,
      headers,
      signal: AbortSignal.timeout(timeoutMs),
    });

    const contentType = upstream.headers.get('content-type');
    if (contentType) res.setHeader('Content-Type', contentType);

    res.status(upstream.status);
    const buf = Buffer.from(await upstream.arrayBuffer());
    res.send(buf);
  } catch (e) {
    console.error(logTag, e);
    res.status(502).json({ message: upstreamErrorMessage });
  }
}
