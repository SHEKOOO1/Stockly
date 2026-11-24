import { Archive, Eye, MoreHorizontal, Pencil, RotateCcw } from 'lucide-react';
import { useEffect, useId, useRef, useState } from 'react';
import { t } from '@/i18n';

export interface ProductRowActionsProps {
  archived: boolean;
  onView: () => void;
  onEdit: () => void;
  onArchive: () => void;
  onRestore: () => void;
}

export function ProductRowActions({
  archived,
  onView,
  onEdit,
  onArchive,
  onRestore,
}: ProductRowActionsProps) {
  const [open, setOpen] = useState(false);
  const [openUpward, setOpenUpward] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const menuId = useId();

  useEffect(() => {
    if (!open) {
      return;
    }

    function handleMouseDown(event: MouseEvent) {
      if (event.target instanceof Node && !containerRef.current?.contains(event.target)) {
        setOpen(false);
      }
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setOpen(false);
      }
    }

    document.addEventListener('mousedown', handleMouseDown);
    window.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('mousedown', handleMouseDown);
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [open]);

  function run(action: () => void) {
    setOpen(false);
    action();
  }

  function toggleMenu() {
    if (!open && buttonRef.current) {
      const buttonRect = buttonRef.current.getBoundingClientRect();
      const scrollArea = buttonRef.current.closest('.data-table__scroll');
      const lowerBound = scrollArea?.getBoundingClientRect().bottom ?? window.innerHeight;
      setOpenUpward(buttonRect.bottom + 176 > lowerBound);
    }
    setOpen((current) => !current);
  }

  return (
    <div
      className={`product-row-menu ${open && openUpward ? 'is-upward' : ''}`.trim()}
      ref={containerRef}
    >
      <button
        ref={buttonRef}
        type="button"
        className="icon-button"
        aria-label={t('warehouse.products.actionsAction')}
        aria-controls={menuId}
        aria-expanded={open}
        onClick={toggleMenu}
      >
        <MoreHorizontal size={17} aria-hidden="true" />
      </button>
      {open ? (
        <div className="product-row-menu__panel" id={menuId} role="menu">
          <button
            type="button"
            role="menuitem"
            className="product-row-menu__item"
            onClick={() => run(onView)}
          >
            <Eye size={14} aria-hidden="true" />
            {t('warehouse.products.viewAction')}
          </button>
          <button
            type="button"
            role="menuitem"
            className="product-row-menu__item"
            disabled={archived}
            onClick={() => run(onEdit)}
          >
            <Pencil size={14} aria-hidden="true" />
            {t('warehouse.products.editAction')}
          </button>
          {archived ? (
            <button
              type="button"
              role="menuitem"
              className="product-row-menu__item"
              onClick={() => run(onRestore)}
            >
              <RotateCcw size={14} aria-hidden="true" />
              {t('warehouse.products.restoreAction')}
            </button>
          ) : (
            <button
              type="button"
              role="menuitem"
              className="product-row-menu__item product-row-menu__item--danger"
              onClick={() => run(onArchive)}
            >
              <Archive size={14} aria-hidden="true" />
              {t('warehouse.products.archiveAction')}
            </button>
          )}
        </div>
      ) : null}
    </div>
  );
}
