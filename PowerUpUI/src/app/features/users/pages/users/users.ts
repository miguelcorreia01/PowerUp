import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { startWith } from 'rxjs';
import { FormsModule } from '@angular/forms';

import { UsersService, UserListItem } from '../../services/users.service';
import { Nav } from '../../../../shared/components/nav/nav';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, Nav],
  templateUrl: './users.html',
  styleUrls: ['./users.css']
})
export class Users {
  private readonly service = inject(UsersService);
  private readonly router = inject(Router);

  searchQuery = signal<string>('');
  
  allUsers = toSignal(
    this.service.getAllUsers().pipe(startWith([] as UserListItem[])),
    { initialValue: [] as UserListItem[] }
  );

  filteredUsers = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const users = this.allUsers();
    
    if (!query) return users;
    
    return users.filter(user => 
      user.name.toLowerCase().includes(query) ||
      user.email.toLowerCase().includes(query)
    );
  });

  onSearchChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchQuery.set(input.value);
  }

  viewProfile(userId: string): void {
    this.router.navigate(['/profile', userId]);
  }

  getRoleLabel(role: string): string {
    return role === 'Instructor' ? 'Instructor'
      : role === 'Admin' ? 'Administrator'
      : 'Member';
  }
}