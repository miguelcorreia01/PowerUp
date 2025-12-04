import { Component, inject, AfterViewInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { startWith } from 'rxjs';
import { Chart, registerables } from 'chart.js';

import { AdminDashboardService, MonthlyDataPoint } from '../../services/admin-dashboard.service';
import { Nav } from '../../../../shared/components/nav/nav';

Chart.register(...registerables);

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, Nav],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.css']
})
export class AdminDashboard implements AfterViewInit {
  private readonly service = inject(AdminDashboardService);

  summary = toSignal(
    this.service.getOverview().pipe(startWith(null)),
    { initialValue: null }
  );

  usersByMonth = toSignal(
    this.service.getUsersByMonth().pipe(startWith([])),
    { initialValue: [] as MonthlyDataPoint[] }
  );

  revenueByMonth = toSignal(
    this.service.getRevenueByMonth().pipe(startWith([])),
    { initialValue: [] as MonthlyDataPoint[] }
  );

  cards = [
    { label: 'Total Users', value: () => this.summary()?.totalUsers ?? 0 },
    { label: 'Members', value: () => this.summary()?.totalMembers ?? 0 },
    { label: 'Instructors', value: () => this.summary()?.totalInstructors ?? 0 },
    { label: 'New Users (30 days)', value: () => this.summary()?.newUsersLast30Days ?? 0 },
    { label: 'Active Subscriptions', value: () => this.summary()?.activeSubscriptions ?? 0 },
    {
      label: 'Monthly Revenue',
      value: () => `$${(this.summary()?.monthlyRevenue ?? 0).toFixed(2)}`
    }
  ];

  private usersChart: Chart | null = null;
  private revenueChart: Chart | null = null;

  ngAfterViewInit(): void {
    setTimeout(() => {
      this.createUsersChart();
      this.createRevenueChart();
    }, 100);
  }

  private createUsersChart(): void {
    const data = this.usersByMonth();
    const ctx = document.getElementById('usersChart') as HTMLCanvasElement;
    
    if (!ctx || data.length === 0) return;

    const labels = data.map(d => {
      const date = new Date(d.year, d.month - 1);
      return date.toLocaleDateString('en-US', { month: 'short', year: 'numeric' });
    });
    const values = data.map(d => d.value);

    if (this.usersChart) {
      this.usersChart.destroy();
    }

    this.usersChart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: labels,
        datasets: [{
          label: 'New Users',
          data: values,
          borderColor: 'rgb(75, 192, 192)',
          backgroundColor: 'rgba(75, 192, 192, 0.2)',
          tension: 0.1,
          fill: true
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true
          },
          title: {
            display: true,
            text: 'New Users Over Time (Last 12 Months)'
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: {
              stepSize: 1
            }
          }
        }
      }
    });
  }

  private createRevenueChart(): void {
    const data = this.revenueByMonth();
    const ctx = document.getElementById('revenueChart') as HTMLCanvasElement;
    
    if (!ctx || data.length === 0) return;

    const labels = data.map(d => {
      const date = new Date(d.year, d.month - 1);
      return date.toLocaleDateString('en-US', { month: 'short', year: 'numeric' });
    });
    const values = data.map(d => d.value);

    if (this.revenueChart) {
      this.revenueChart.destroy();
    }

    this.revenueChart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: labels,
        datasets: [{
          label: 'Revenue ($)',
          data: values,
          backgroundColor: 'rgba(54, 162, 235, 0.6)',
          borderColor: 'rgba(54, 162, 235, 1)',
          borderWidth: 1
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true
          },
          title: {
            display: true,
            text: 'Monthly Revenue (Last 12 Months)'
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: {
              callback: function(value) {
                return '$' + (value as number).toFixed(2);
              }
            }
          }
        }
      }
    });
  }
}