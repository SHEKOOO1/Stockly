import { common } from './common';
import { organization } from './organization';
import { platform } from './platform';
import { warehouse } from './warehouse';

export const translations = {
  common,
  platform,
  organization,
  warehouse,
} as const;

export type TranslationValues = Readonly<Record<string, string | number>>;

type Primitive = string | number | boolean;

type DottedKey<Prefix extends string, Key extends string | number> = Prefix extends ''
  ? `${Key}`
  : `${Prefix}.${Key}`;

type Leaves<Value, Prefix extends string = ''> = {
  [Key in keyof Value & (string | number)]: Value[Key] extends Primitive
    ? DottedKey<Prefix, Key>
    : Leaves<Value[Key], DottedKey<Prefix, Key>>;
}[keyof Value & (string | number)];

export type TranslationKey = Leaves<typeof translations>;

export const locale = 'ar-EG-u-nu-latn';

function lookup(key: string): unknown {
  return key.split('.').reduce<unknown>((current, segment) => {
    if (current && typeof current === 'object' && segment in current) {
      return (current as Record<string, unknown>)[segment];
    }
    return undefined;
  }, translations);
}

export function t(key: TranslationKey, values?: TranslationValues): string {
  const found = lookup(key);
  const template = typeof found === 'string' ? found : key;

  if (!values) {
    return template;
  }

  return template.replace(/\{(\w+)\}/g, (match, name: string) => {
    const value = values[name];
    return value === undefined ? match : String(value);
  });
}
