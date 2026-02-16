const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim() ?? ''

function buildUrl(path: string): string {
  if (!apiBaseUrl) {
    return path
  }

  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return `${apiBaseUrl.replace(/\/$/, '')}${normalizedPath}`
}

export async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(buildUrl(path), {
    headers: {
      Accept: 'application/json'
    }
  })

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`)
  }

  return (await response.json()) as T
}
