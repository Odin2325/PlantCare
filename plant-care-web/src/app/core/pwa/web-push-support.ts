// Feature detection also covers iPhone/iPad browser tabs where Web Push is not
// exposed. Do not request permission merely to discover platform support.
export function supportsWebPush(): boolean {
  return typeof window !== 'undefined' && typeof navigator !== 'undefined'
    && typeof navigator.serviceWorker !== 'undefined' && typeof window.PushManager !== 'undefined'
    && typeof Notification !== 'undefined';
}
