import { memo } from 'react';

function IconButtonComponent({ label, children, className = '', ...props }) {
  return (
    <button
      className={`icon-button ${className}`}
      aria-label={label}
      title={label}
      {...props}
    >
      {children}
    </button>
  );
}

export const IconButton = memo(IconButtonComponent);
