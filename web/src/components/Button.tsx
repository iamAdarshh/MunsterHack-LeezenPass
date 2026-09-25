import type { ButtonHTMLAttributes } from 'react'
import { type ButtonVariant, buttonClass } from './buttonClass'
import { Spinner } from './States'

interface Props extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  fullWidth?: boolean
  loading?: boolean
}

export function Button({ variant = 'primary', fullWidth, loading, disabled, children, type = 'button', ...rest }: Props) {
  return (
    <button type={type} className={buttonClass(variant, fullWidth)} disabled={disabled || loading} {...rest}>
      {loading && <Spinner className="size-4" />}
      {children}
    </button>
  )
}
