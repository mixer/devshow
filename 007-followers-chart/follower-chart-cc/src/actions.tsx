import { BehaviorSubject } from 'rxjs';
import { Chart } from 'chart.js';

import { FollowerStateHistory } from '../../game-client/src/live_loader';
let chart: Chart;

const followerRecord: BehaviorSubject<FollowerStateHistory> = new BehaviorSubject({});

followerRecord
    .subscribe((points: FollowerStateHistory) => {
        if (!chart || !chart.data) {
            return;
        }

        chart.data.labels = [];
        chart.data.datasets.forEach((dataset: any) => {
            dataset.data = [];
        });

        Object.keys(points).forEach(point => {
            // need to format the timestamp smaller
            chart.data.labels.push(new Date(points[point].timestamp).toLocaleTimeString());
            chart.data.datasets.forEach((dataset: any) => {
                dataset.data.push(points[point].count);
            });
            chart.update();
        });
    });

export function updateHistory(history: FollowerStateHistory) {
    followerRecord.next(history);
}

export function initChart() {
    var ctx = (document.getElementById('follower-chart') as HTMLCanvasElement).getContext('2d');
    chart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: [],
            datasets: [{
                label: "Following Chart",
                data: [],
            }]
        },
        options: {
            scales: {
                yAxes: [{
                    ticks: {
                        beginAtZero: true
                    }
                }]
            }
        }
    });
}