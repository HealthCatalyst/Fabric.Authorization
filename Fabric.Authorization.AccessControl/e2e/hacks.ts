import { browser } from 'protractor';

// Taken from from Atlas project
/**
 * This is a HACK to tell Protractor that it's OK to continue the test.
 * For some reason, in Atlas the NgZone will never say it has no pending
 * macro tasks, so we go in and hard code that to `false`
 */
export async function tellNgZoneItHasNoPendingMacroTasks() {
    await browser.waitForAngularEnabled(false);
    const script = `
               const __testability = window.getAngularTestability(
                   document.querySelector('app-root')
               );
               __testability._ngZone.hasPendingMacrotasks = false;
               __testability.whenStable(() => console.log('zone stable'));
           `;

    await browser.executeScript(script);
    await browser.waitForAngularEnabled(true);
}
