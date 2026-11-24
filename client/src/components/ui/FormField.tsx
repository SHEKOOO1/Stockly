import { cloneElement, isValidElement, useId, type ReactNode } from 'react';
import { t, type TranslationKey } from '@/i18n';

export interface FormFieldProps {
  labelKey: TranslationKey;
  hintKey?: TranslationKey;
  errorKey?: TranslationKey;
  required?: boolean;
  htmlFor?: string;
  className?: string;
  children: ReactNode;
}

export function FormField({
  labelKey,
  hintKey,
  errorKey,
  required = false,
  htmlFor,
  className = '',
  children,
}: FormFieldProps) {
  const generatedId = useId();
  const fieldId = htmlFor ?? generatedId;
  const hintId = `${generatedId}-hint`;
  const errorId = `${generatedId}-error`;
  const control =
    isValidElement<{ id?: string }>(children) && !children.props.id
      ? cloneElement(children, { id: fieldId })
      : children;

  return (
    <div className={`form-field ${className}`.trim()}>
      <label className="form-field__label" htmlFor={fieldId}>
        {t(labelKey)}
        {required ? (
          <span className="form-field__required" aria-hidden="true">
            *
          </span>
        ) : null}
      </label>
      {control}
      {hintKey ? (
        <span id={hintId} className="form-field__hint">
          {t(hintKey)}
        </span>
      ) : null}
      {errorKey ? (
        <span id={errorId} className="form-field__error" role="alert">
          {t(errorKey)}
        </span>
      ) : null}
    </div>
  );
}
