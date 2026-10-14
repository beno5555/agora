import assert from 'node:assert/strict';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';
import { build } from 'esbuild';
import '@angular/compiler';
import { createEnvironmentInjector, runInInjectionContext } from '@angular/core';
import { Keypair, SystemProgram, Transaction } from '@solana/web3.js';

// Compile the real Angular services with the build tool already supplied by @angular/build.
const frontend = fileURLToPath(new URL('../', import.meta.url));
const compiled = new URL('../node_modules/.cache/agora-tests/wallet-core.mjs', import.meta.url);
await build({
  stdin: {
    contents: `
      export { errorText } from './src/app/core/error-text';
      export { ApiError, ApiService } from './src/app/core/api.service';
      export { WalletService, WalletRejectedError } from './src/app/core/wallet.service';
      export { DealActionService } from './src/app/core/deal-action.service';
      export { AuthService } from './src/app/core/auth.service';
      export { ToastService } from './src/app/core/toast.service';
    `,
    resolveDir: frontend,
    loader: 'ts',
  },
  bundle: true,
  packages: 'external',
  platform: 'node',
  format: 'esm',
  tsconfig: fileURLToPath(new URL('../tsconfig.json', import.meta.url)),
  outfile: fileURLToPath(compiled),
});
const {
  errorText,
  ApiError,
  ApiService,
  WalletService,
  WalletRejectedError,
  DealActionService,
  AuthService,
  ToastService,
} = await import(compiled.href);

for (const message of [
  'Unexpected error',
  'Transaction simulation failed',
  'Blockhash not found',
  'Failed to send transaction',
]) {
  test(`errorText: ${message} does not claim the wallet is on mainnet`, () => {
    const text = errorText(new Error(message));
    assert.doesNotMatch(text, /is on the main network/);
    assert.doesNotMatch(text, /\[object Object\]/);
    assert.equal(errorText({ code: -32003, message }), text);
  });
}

test('errorText preserves actionable funding and API errors', () => {
  assert.match(errorText(new Error('insufficient lamports')), /Not enough test money/);
  assert.equal(errorText(new ApiError('Listing Not Found', 404)), 'Listing Not Found');
});

test('WalletService matches the backend commitment without skipping preflight', async () => {
  let receivedOptions;
  const transaction = new Transaction();
  globalThis.window = {
    phantom: {
      solana: {
        on() {},
        async signAndSendTransaction(tx, options) {
          assert.equal(tx, transaction);
          receivedOptions = options;
          return { signature: 'test-signature' };
        },
      },
    },
  };

  assert.equal(await new WalletService().signAndSendTransaction(transaction), 'test-signature');
  assert.equal(receivedOptions.preflightCommitment, 'confirmed');
  assert.notEqual(receivedOptions.skipPreflight, true);
});

test('WalletService keeps user rejection separate from RPC failures', async () => {
  globalThis.window = {
    phantom: {
      solana: {
        on() {},
        async signAndSendTransaction() {
          throw { code: 4001, message: 'User rejected' };
        },
      },
    },
  };

  await assert.rejects(
    new WalletService().signAndSendTransaction(new Transaction()),
    WalletRejectedError,
  );
});

test('Buy shows preparation, wallet approval and confirmation; duplicate clicks do not create drafts', async () => {
  const buyer = Keypair.generate().publicKey;
  const transaction = new Transaction({
    feePayer: buyer,
    recentBlockhash: Keypair.generate().publicKey.toBase58(),
  }).add(
    SystemProgram.transfer({
      fromPubkey: buyer,
      toPubkey: Keypair.generate().publicKey,
      lamports: 1,
    }),
  );
  const encoded = transaction
    .serialize({ requireAllSignatures: false, verifySignatures: false })
    .toString('base64');
  const prepared = Promise.withResolvers();
  const signed = Promise.withResolvers();
  const synced = Promise.withResolvers();
  const calledWallet = Promise.withResolvers();
  const calledSync = Promise.withResolvers();
  let creates = 0;
  const injector = createEnvironmentInjector([
    {
      provide: ApiService,
      useValue: {
        post(path) {
          if (path === '/api/deals') {
            creates++;
            return prepared.promise;
          }
          assert.equal(path, '/api/deals/deal-id/sync');
          calledSync.resolve();
          return synced.promise;
        },
      },
    },
    {
      provide: WalletService,
      useValue: {
        publicKey: () => buyer.toBase58(),
        signAndSendTransaction() {
          calledWallet.resolve();
          return signed.promise;
        },
      },
    },
    { provide: AuthService, useValue: { user: () => ({ wallet: buyer.toBase58() }) } },
    {
      provide: ToastService,
      useValue: {
        fromError(error) {
          throw error;
        },
      },
    },
  ]);
  try {
    const actions = runInInjectionContext(injector, () => new DealActionService());
    const buying = actions.buy('listing-id');
    assert.equal(actions.phase(), 'preparing');
    assert.equal(await actions.buy('listing-id'), null);
    prepared.resolve({ deal: { id: 'deal-id' }, transaction: encoded });
    await calledWallet.promise;
    assert.equal(actions.phase(), 'wallet');
    signed.resolve('test-signature');
    await calledSync.promise;
    assert.equal(actions.phase(), 'confirming');
    synced.resolve({ id: 'deal-id', status: 'AwaitingSeller' });
    assert.equal((await buying).status, 'AwaitingSeller');
    assert.equal(actions.phase(), 'idle');
    assert.equal(creates, 1);
  } finally {
    injector.destroy();
  }
});
