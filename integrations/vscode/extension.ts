import * as vscode from 'vscode';
import { execFile } from 'node:child_process';

function runCli(args: string[], input?: string): Promise<string> {
  const cli = vscode.workspace.getConfiguration('aer').get<string>('cliPath', 'aer');
  return new Promise((resolve, reject) => {
    const child = execFile(cli, args, { maxBuffer: 16 * 1024 * 1024 }, (error, stdout, stderr) => {
      if (error) reject(new Error(stderr || error.message));
      else resolve(stdout);
    });
    if (input !== undefined) child.stdin?.end(input);
  });
}

export function activate(context: vscode.ExtensionContext) {
  const register = (command: string, handler: () => Promise<void>) =>
    context.subscriptions.push(vscode.commands.registerCommand(command, handler));

  register('aer.validate', async () => {
    const doc = vscode.window.activeTextEditor?.document;
    if (!doc) return;
    try {
      await runCli(['validate', '-'], doc.getText());
      vscode.window.showInformationMessage('AER: document is valid.');
    } catch (e) {
      vscode.window.showErrorMessage(`AER validation failed: ${e instanceof Error ? e.message : String(e)}`);
    }
  });

  register('aer.format', async () => {
    const editor = vscode.window.activeTextEditor;
    if (!editor) return;
    try {
      const formatted = await runCli(['fmt', '-'], editor.document.getText());
      await editor.edit(edit => edit.replace(
        new vscode.Range(editor.document.positionAt(0), editor.document.positionAt(editor.document.getText().length)),
        formatted));
    } catch (e) {
      vscode.window.showErrorMessage(`AER format failed: ${e instanceof Error ? e.message : String(e)}`);
    }
  });

  register('aer.toJson', async () => {
    const editor = vscode.window.activeTextEditor;
    if (!editor) return;
    try {
      const json = await runCli(['convert', '-', '--to', 'json'], editor.document.getText());
      await vscode.workspace.openTextDocument({ language: 'json', content: json }).then(vscode.window.showTextDocument);
    } catch (e) {
      vscode.window.showErrorMessage(`AER conversion failed: ${e instanceof Error ? e.message : String(e)}`);
    }
  });

  register('aer.toAer', async () => {
    const editor = vscode.window.activeTextEditor;
    if (!editor) return;
    try {
      const aer = await runCli(['convert', '-', '--to', 'aer'], editor.document.getText());
      await vscode.workspace.openTextDocument({ language: 'aer', content: aer }).then(vscode.window.showTextDocument);
    } catch (e) {
      vscode.window.showErrorMessage(`AER conversion failed: ${e instanceof Error ? e.message : String(e)}`);
    }
  });

  register('aer.benchmark', async () => {
    const doc = vscode.window.activeTextEditor?.document;
    if (!doc) return;
    try {
      const result = await runCli(['benchmark', '-'], doc.getText());
      const output = vscode.window.createOutputChannel('AER Benchmark');
      output.clear(); output.appendLine(result); output.show();
    } catch (e) {
      vscode.window.showErrorMessage(`AER benchmark failed: ${e instanceof Error ? e.message : String(e)}`);
    }
  });
}

export function deactivate() {}
