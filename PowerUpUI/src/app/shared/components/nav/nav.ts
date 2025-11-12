import { Component, ChangeDetectionStrategy, HostListener, inject, signal, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-nav',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './nav.html',
  styleUrls: ['./nav.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Nav {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly host = inject(ElementRef<HTMLElement>);
  
  
userMenuOpen = signal(false);

 toggleUserMenu(event: MouseEvent): void {
     event.stopPropagation();
     this.userMenuOpen.update(open => !open);
   }

  closeUserMenu(): void {
    this.userMenuOpen.set(false);
  }

  async profile(): Promise<void> {
    this.closeUserMenu();
    await this.router.navigate(['/profile']);
  }

  async logout(): Promise<void> {
    this.auth.logout();
    this.closeUserMenu();
    await this.router.navigate(['/login']);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.userMenuOpen()) return;
     const target = event.target as Node | null;
    if (!target) return;
    if (this.host.nativeElement.contains(target)) return;
    this.closeUserMenu();
  }
  
}