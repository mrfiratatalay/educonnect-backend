# Frontend and design

The UI lives in a separate repository: [educonnect-frontend](https://github.com/mrfiratatalay/educonnect-frontend).
We built it with React 19, TypeScript and Vite 8, on top of the Ant Design 6 component library. This document covers
how the frontend is structured, how we manage server data, sessions, the design language and mobile.

![Personalized feed on desktop](../screenshots/feed.webp)

## Technologies

| Job | Choice | Why? |
|---|---|---|
| UI | React 19.2, TypeScript 5.9 | Meeting the backend contracts with types |
| Build | Vite 8 | A fast dev server; a second build with `--mode demo` |
| Components | Ant Design 6.3, lucide-react | Ready-made forms, drawers and tabs; the whole theme managed from one file |
| Server data | TanStack Query 5.95 | Caching, loading states, infinite scroll, optimistic updates |
| App state | Zustand 5 | Small stores for the few global states: session, theme, menus |
| Routing | React Router 6.30 | Nested layouts, protected routes |
| HTTP and real time | axios 1.13, @microsoft/signalr 10 | Being able to swap the transport layer in one place; the demo is built on this |
| Forms | React Hook Form 7.72, zod 4.3 | Form state separate from the validation schema |
| EduAI answers | react-markdown, remark-gfm | Answers arrive as Markdown |

`recharts` is installed but no screen uses it. It was added for an admin analytics screen that we never wrote.

## Folder structure

```
src/
├── pages/        22 page files; 23 routes in App.tsx
├── features/     11 modules; each has requests (api.ts), most have query hooks (hooks.ts)
├── components/   the app shell (menu, top bar, mobile navigation), the EduAI panel, shared components
├── store/        5 Zustand stores: session, theme, UI, chat, post composer
├── lib/          apiClient.ts, realtime.ts, signalrLifecycle.ts
├── theme/        antdTheme.ts: every color and size
└── demo/         the in-browser demo backend; only part of the demo build
```

The modules mirror the backend controllers: `posts`, `groups`, `events`, `products`, `messages`, `notifications`,
`chat` and so on. A screen never calls axios directly; it gets its data through the module's hook. In total the UI
calls 82 distinct endpoints.

## Server state

**Problem.** Managing loading states, caching and "when should this data be refreshed?" by hand for 82 endpoints
would have been error-prone.

**Solution.** All data from the server lives in TanStack Query. The defaults are in
[main.tsx](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/main.tsx): data stays fresh for 5
minutes, and a failed query is retried once.

| Pattern | Where? |
|---|---|
| Infinite scroll (`useInfiniteQuery`) | Feeds, community posts, messages |
| Periodic refetch | Messages every 30 seconds, notifications every 60 seconds; a safety net next to SignalR |
| Optimistic updates | Likes, bookmarks, follows, creating posts, sending messages |

**An optimistic like.** When you press like, the count changes without waiting for the server
([posts/hooks.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/features/posts/hooks.ts),
`useTogglePostLikeMutation`):

1. Post queries are cancelled and a snapshot of the cache is taken.
2. The post is updated everywhere at once: in feed pages, on the detail page and in the community feed.
3. If the request fails, the snapshot is restored. Either way, the affected queries are refetched from the server.

**Why.** The user sees the result of the click immediately, while the true count still comes from the server. The
unique index in the backend also keeps two quick clicks from inflating the counter ([Database](04-database.md)).

**Cost.** `updatePostAcrossCaches` has to know every cache shape a post can live in. When a new post list is added, it
has to be added there too; otherwise that list keeps the old count.

## Sessions

- **The access token lives only in memory.**
  [token.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/features/auth/token.ts) holds a single
  module variable; the token is never written to `localStorage`, so there is no long-lived token to read from
  browser storage.
- **Restoring the session on startup.** When the app opens,
  [authStore.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/store/authStore.ts) calls
  `POST /api/auth/refresh`; if the HttpOnly cookie is valid, a new token arrives. Meanwhile protected routes show a
  loading indicator instead of sending the user to the login page.
- **A single refresh.** If several expired requests get a 401 at the same time, they share one refresh request
  ([authenticatedRequest.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/features/auth/authenticatedRequest.ts)).
  Refresh tokens are single-use in the backend; with two parallel refreshes, one would get a 401 and end the session.
- **A clean logout.** On logout, open SignalR connections are stopped first
  ([signalrLifecycle.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/lib/signalrLifecycle.ts));
  otherwise they could try to reconnect with the old token.

**Cost.** Because the token lives in memory, every page reload means a refresh request. The cookie problem in
production is in the table at the end of [Security](06-security.md): the UI and the API were on different sites, so
the `SameSite=Lax` cookie was not sent and a reload ended the session.

## The transport layer in one place

Every module gets its HTTP client from
[apiClient.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/lib/apiClient.ts) and its realtime
connection from [realtime.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/lib/realtime.ts):

```ts
export function createApiClient() {
  const client = axios.create({ baseURL: API_BASE_URL, withCredentials: true });
  if (__DEMO_MODE__) {
    client.defaults.adapter = async (config) => {
      const { handleDemoRequest } = await import("@/demo/adapter");
      return handleDemoRequest(config);
    };
  }
  return client;
}
```

In real mode SignalR uses WebSockets only, passes the token through `accessTokenFactory` and, if the connection drops,
retries after 0, 2, 5, 10 and 30 seconds. In demo mode the same interface is fed by an in-browser event bus. Screens
do not know which mode they are running in. The whole demo design rests on these two files; details in
[Deployment strategy](09-deployment-strategy.md).

## Design language

On 18 April 2026 we redesigned the UI in a language we called "deep teal". Every color and size lives in one file:
[antdTheme.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/theme/antdTheme.ts).

| Element | Value |
|---|---|
| Primary color | `#0D9488` (teal); links and the selected menu item `#0F766E` |
| Font | Manrope, falling back to Inter |
| Corners | 10 px in general, 14 px for cards and dialogs |
| Heights | Buttons 38 px, inputs 42 px, menu items 48 px |
| Light theme | Slate tones: background `#F1F5F9`, white cards |
| Dark theme | Deep navy rather than black: background `#0F172A`, cards `#1E293B` |

These tokens are passed to Ant Design's light and dark algorithms. The user's choice is kept in `localStorage`
([themeStore.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/store/themeStore.ts)). Ant
Design's CSS variables use the `edu` prefix and class names are not hashed, which makes the UI easy to inspect in the
browser's developer tools.

![Dark theme](../screenshots/dark.webp)

The code is in English; menus, empty states and error messages are in Turkish.

## Mobile

We expected most students to open the platform on their phones, so we handled every screen in a narrow layout too.
28 components and pages read the screen width through Ant Design's `Grid.useBreakpoint` hook, and `index.css` has
separate rules below 767 px.

- The left menu on desktop becomes a bottom navigation bar and a drawer on phones
  ([MobileNav.tsx](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/src/components/layout/MobileNav.tsx)).
- The compose button becomes a floating button on phones.
- The EduAI panel and the chat bubbles change their layout on narrow screens.

| Feed | Community | EduAI |
|---|---|---|
| ![Feed on a phone](../screenshots/mobile-feed.webp) | ![Community on a phone](../screenshots/mobile-community.webp) | ![EduAI on a phone](../screenshots/mobile-eduai.webp) |

## Accessibility and error states

Accessibility is an area where this project is not strong. Ant Design components bring their own keyboard and focus
support, but our own icon buttons have only 11 `aria-label`s, and we never ran an accessibility audit (Lighthouse or
axe).

An `ErrorBoundary` at the outermost level keeps an unexpected UI error from turning the whole app into a white
screen. For API errors the user sees the `detail` field of Problem Details or the `message` field of a business rule
error; the full error contract is in [Security](06-security.md).

## Bundle size

**Where it stands.** Every page is imported directly in `App.tsx`; routes are not lazy-loaded. In the real build the
main bundle is a single chunk: 2,157 KB, 639 KB gzipped.

**Why it stayed like this.** During the thesis our priority was finishing features; we never measured the first load
time.

**How it gets fixed.** Route-level splitting with `React.lazy`, separate chunks for Ant Design and the Markdown
libraries, and removing the unused `recharts` package.

The demo code is not in this bundle. `__DEMO_MODE__` turns into a constant at build time
([vite.config.ts](https://github.com/mrfiratatalay/educonnect-frontend/blob/main/vite.config.ts)). In the real build
its value is `false`, so the demo branches and their dynamic imports are removed from the bundle entirely. I verified
this by searching the built files.

## What I learned on this side

- Once server state is separated from UI state, most of the need for global state disappears: 5 small Zustand stores
  were enough.
- Optimistic updates look easy. The hard part is thinking about rolling back on errors and about every copy of the
  same data in the cache.
- Keeping the transport layer in one file let me build the demo mode months later without touching a single screen.
