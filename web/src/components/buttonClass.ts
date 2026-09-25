export type ButtonVariant = 'primary' | 'secondary' | 'danger'

const variants: Record<ButtonVariant, string> = {
  primary: 'bg-brand-700 text-white hover:bg-brand-800 disabled:bg-brand-700/60',
  secondary: 'border border-slate-300 bg-white text-slate-800 hover:bg-slate-50 disabled:text-slate-400',
  danger: 'border border-red-300 bg-white text-red-700 hover:bg-red-50 disabled:text-red-300',
}

/** Shared class list so links can look like buttons. */
export function buttonClass(variant: ButtonVariant = 'primary', fullWidth = false) {
  return `inline-flex min-h-12 items-center justify-center gap-2 rounded-lg px-4 font-semibold transition-colors ${
    variants[variant]
  } ${fullWidth ? 'w-full' : ''}`
}
