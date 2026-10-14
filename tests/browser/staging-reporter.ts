import { writeFileSync } from 'node:fs';
import type { Reporter, TestCase, TestResult } from '@playwright/test/reporter';

// Raw Playwright errors can contain authenticated headers and private links.
export default class SafeReporter implements Reporter {
  private results: object[] = [];
  onTestEnd(test: TestCase, result: TestResult) {
    console.log(`${result.status.toUpperCase()}: ${test.title} (${test.parent.project()?.name})`);
    if (result.errors.length) {
      const stage = test.annotations.filter(a => a.type === 'stage').at(-1)?.description ?? 'unspecified';
      // Classify only known codes; never emit error text, call logs or headers.
      const messages = result.errors.map(e => e.message ?? '').join('\n').replace(/\u001b\[[0-9;]*m/g, '');
      const http = messages.match(/Received:\s*(400|401|403|404|429|500|502|503|504)\b/)?.[1];
      const network = messages.match(/\b(ERR_NAME_NOT_RESOLVED|ENOTFOUND|ERR_CONNECTION_REFUSED|ERR_CONNECTION_RESET)\b/)?.[1];
      const code = http ? `http-${http}` : network ?? (/toBeVisible/.test(messages) ? 'visibility-assertion' : /toHaveURL/.test(messages) ? 'route-assertion' : /timeout/i.test(messages) ? 'timeout' : 'assertion');
      console.log(`Browser check failed at ${/^[a-z-]+$/.test(stage) ? stage : 'unspecified'} (${code}); private diagnostics withheld.`);
    }
    this.results.push({ title: test.title, project: test.parent.project()?.name, status: result.status, durationMs: result.duration });
  }
  onError() { console.log('Browser runner failed; private diagnostics withheld.'); }
  onEnd() { writeFileSync('/private/browser-results.json', JSON.stringify(this.results, null, 2)); }
}
